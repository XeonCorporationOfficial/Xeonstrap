set -euo pipefail
umask 022

ROOT="$(cd "$(dirname "$0")/../../.." && pwd)"
RID="${1:?A macOS runtime identifier is required}"
VERSION="${2:-}"
OUTPUT="${3:?An output directory is required}"
PUBLISHED_EXECUTABLE="${4:-}"

case "$RID" in
  osx-x64|osx-arm64) ;;
  *) echo "Unsupported macOS runtime identifier"; exit 1 ;;
esac

if [ -z "$VERSION" ]; then
  VERSION="$(sed -n 's:^[[:space:]]*<XeonstrapVersion>\(.*\)</XeonstrapVersion>[[:space:]]*$:\1:p' "$ROOT/Directory.Build.props" | head -n 1)"
fi

if [[ ! "$VERSION" =~ ^[0-9]+([.][0-9]+){1,3}$ ]]; then
  echo "The package version is invalid"
  exit 1
fi

mkdir -p "$OUTPUT"
OUTPUT="$(cd "$OUTPUT" && pwd)"
DMG_TARGET="$OUTPUT/Xeonstrap-$RID.dmg"
ARCHIVE_TARGET="$OUTPUT/Xeonstrap-$RID.zip"
TARBALL_TARGET="$OUTPUT/Xeonstrap-$RID.tar.gz"

if [ -e "$DMG_TARGET" ] || [ -e "$ARCHIVE_TARGET" ] || [ -e "$TARBALL_TARGET" ]; then
  echo "The requested output already exists"
  exit 1
fi

LOCK="$OUTPUT/.xeonstrap-macos-$RID.lock"
if ! mkdir "$LOCK" 2>/dev/null; then
  echo "Another package operation is already running"
  exit 1
fi
STAGE=""

cleanup() {
  if [ -n "$STAGE" ] && [ -e "$STAGE" ]; then
    rm -rf "$STAGE"
  fi
  rmdir "$LOCK" 2>/dev/null || true
}

trap cleanup EXIT
STAGE="$(mktemp -d "${TMPDIR:-/tmp}/xeonstrap-macos.XXXXXX")"
PUBLISH="$STAGE/publish"
APPLICATION="$STAGE/Xeonstrap.app"
DMG="$STAGE/Xeonstrap-$RID.dmg"
ARCHIVE="$STAGE/Xeonstrap-$RID.zip"
ISO="$STAGE/Xeonstrap-$RID.iso"

commit_artifact() {
  mv -n "$1" "$2"
  if [ -e "$1" ]; then
    echo "The requested output already exists"
    exit 1
  fi
}

mkdir -p "$PUBLISH"
if [ -n "$PUBLISHED_EXECUTABLE" ]; then
  [ -f "$PUBLISHED_EXECUTABLE" ] && [ -s "$PUBLISHED_EXECUTABLE" ] || { echo "The published Xeonstrap executable is unavailable"; exit 1; }
  cp "$PUBLISHED_EXECUTABLE" "$PUBLISH/Xeonstrap"
else
  dotnet publish "$ROOT/src/Xeonstrap.Cross/Xeonstrap.Cross.csproj" -c Release -r "$RID" --self-contained true -o "$PUBLISH" -p:XeonstrapLinuxPackagingRoot="$STAGE/" -p:Version="$VERSION" -p:DebugType=none -p:DebugSymbols=false
fi
mkdir -p "$APPLICATION/Contents/MacOS" "$APPLICATION/Contents/Resources"
cp -R "$PUBLISH/." "$APPLICATION/Contents/MacOS/"
case "$RID" in
  osx-arm64) RUST_TARGET=aarch64-apple-darwin ;;
  osx-x64) RUST_TARGET=x86_64-apple-darwin ;;
esac
if [ "$(uname -s)" = "Darwin" ]; then
  rustup target add "$RUST_TARGET"
  CARGO_TARGET_DIR="$STAGE/rust" RUSTFLAGS="--remap-path-prefix=$ROOT=/src --remap-path-prefix=${CARGO_HOME:-$HOME/.cargo}=/cargo" cargo build --manifest-path "$ROOT/src/VirtualDisplay/Cargo.toml" --locked --release --target "$RUST_TARGET"
  cp "$STAGE/rust/$RUST_TARGET/release/xeonstrap-virtualdisplay" "$APPLICATION/Contents/MacOS/xeonstrap-virtualdisplay"
  chmod 755 "$APPLICATION/Contents/MacOS/xeonstrap-virtualdisplay"
elif [ -s "${XEONSTRAP_VIRTUALDISPLAY_BINARY:-$ROOT/external/virtualdisplay/$RID/xeonstrap-virtualdisplay}" ]; then
  if [ -z "${XEONSTRAP_VIRTUALDISPLAY_BINARY:-}" ]; then
    (cd "$ROOT" && sha256sum -c external/virtualdisplay/SOURCES.sha256)
    (cd "$ROOT/external/virtualdisplay/$RID" && sha256sum -c SHA256SUMS)
  fi
  cp "${XEONSTRAP_VIRTUALDISPLAY_BINARY:-$ROOT/external/virtualdisplay/$RID/xeonstrap-virtualdisplay}" "$APPLICATION/Contents/MacOS/xeonstrap-virtualdisplay"
  chmod 755 "$APPLICATION/Contents/MacOS/xeonstrap-virtualdisplay"
else
  echo "A macOS virtual display binary is required for this package"
  exit 1
fi
mkdir -p "$APPLICATION/Contents/Resources/licenses"
cp "$ROOT/THIRD-PARTY-NOTICES.txt" "$ROOT/legal/sources/Bibata-Cursor-v2.0.7.tar.gz" "$APPLICATION/Contents/Resources/licenses/"
chmod 755 "$APPLICATION/Contents/Resources/licenses"
chmod 644 "$APPLICATION/Contents/Resources/licenses/"*
cp "$ROOT/src/VirtualDisplay/THIRD-PARTY-NOTICES.txt" "$APPLICATION/Contents/Resources/VirtualDisplay-Notices.txt"
cp "$ROOT/LICENSE.XEONSTRAP" "$APPLICATION/Contents/Resources/VirtualDisplay-License.txt"
chmod 644 "$APPLICATION/Contents/Resources/VirtualDisplay-Notices.txt" "$APPLICATION/Contents/Resources/VirtualDisplay-License.txt"
NOTICES="$APPLICATION/Contents/MacOS/LibreWPF/Notices"
if [ -d "$NOTICES" ]; then
  while IFS= read -r directory; do
    base="$(basename "$directory")"
    case "$base" in
      *.*) mv "$directory" "$(dirname "$directory")/${base//./-}" ;;
    esac
  done < <(find "$NOTICES" -depth -type d)
fi
cp "$ROOT/build/Packaging/MacOS/Info.plist" "$APPLICATION/Contents/Info.plist"
cp "$ROOT/build/Packaging/MacOS/Xeonstrap.icns" "$APPLICATION/Contents/Resources/Xeonstrap.icns"
cp "$ROOT/src/Xeonstrap.App/Xeonstrap.png" "$APPLICATION/Contents/Resources/Xeonstrap.png"
chmod 755 "$APPLICATION/Contents/MacOS/Xeonstrap" "$APPLICATION/Contents/MacOS/xeonstrap-virtualdisplay"

if [ "$(uname -s)" != "Darwin" ]; then
  if [ -n "${MACOS_SIGN_IDENTITY:-}" ] || [ -n "${MACOS_NOTARY_PROFILE:-}" ]; then
    echo "Developer ID signing and notarization require packaging on macOS"
    exit 1
  fi
  sed -i -e "/<key>CFBundleShortVersionString<\/key>/{n;s|<string>[^<]*</string>|<string>$VERSION</string>|}" -e "/<key>CFBundleVersion<\/key>/{n;s|<string>[^<]*</string>|<string>$VERSION</string>|}" "$APPLICATION/Contents/Info.plist"
  chmod 644 "$APPLICATION/Contents/Info.plist"
  chmod -R go-w "$APPLICATION"
  case "$(uname -s):$(uname -m)" in
    Linux:x86_64)
      SIGNER_TARGET=x86_64-unknown-linux-musl
      SIGNER_HASH=dbe85cedd8ee4217b64e9a0e4c2aef92ab8bcaaa41f20bde99781ff02e600002
      ;;
    Linux:aarch64|Linux:arm64)
      SIGNER_TARGET=aarch64-unknown-linux-musl
      SIGNER_HASH=4af92c87ddf52f5f2d1258a3b4e56c7dcb8f1b2468df744976c5f139e031961f
      ;;
    MINGW*:x86_64|MSYS*:x86_64|CYGWIN*:x86_64)
      SIGNER_TARGET=x86_64-pc-windows-msvc
      SIGNER_HASH=54bb500e2da7a8de02fcae0f331d1cac6e6d7173b4281042ff9c528ba3159aaa
      ;;
    *) echo "This host cannot sign macOS app bundles"; exit 1 ;;
  esac
  SIGNER_RELEASE="apple-codesign-0.29.0-$SIGNER_TARGET"
  SIGNER_EXTENSION=tar.gz
  case "$SIGNER_TARGET" in *windows*) SIGNER_EXTENSION=zip ;; esac
  SIGNER_CACHE="${XDG_CACHE_HOME:-$HOME/.cache}/xeonstrap/rcodesign"
  SIGNER_ARCHIVE="$SIGNER_CACHE/$SIGNER_RELEASE.$SIGNER_EXTENSION"
  mkdir -p "$SIGNER_CACHE"
  if command -v flock >/dev/null 2>&1; then
    exec 9>"$SIGNER_CACHE/.download.lock"
    flock 9
  fi
  if ! printf '%s  %s\n' "$SIGNER_HASH" "$SIGNER_ARCHIVE" | sha256sum -c - >/dev/null 2>&1; then
    rm -f "$SIGNER_ARCHIVE"
    curl --fail --location --retry 8 --retry-delay 10 --retry-all-errors --connect-timeout 30 --max-time 300 --output "$SIGNER_ARCHIVE.part" "https://github.com/indygreg/apple-platform-rs/releases/download/apple-codesign/0.29.0/$SIGNER_RELEASE.$SIGNER_EXTENSION"
    mv "$SIGNER_ARCHIVE.part" "$SIGNER_ARCHIVE"
  fi
  if command -v flock >/dev/null 2>&1; then
    exec 9>&-
  fi
  printf '%s  %s\n' "$SIGNER_HASH" "$SIGNER_ARCHIVE" | sha256sum -c -
  mkdir "$STAGE/signer"
  if [ "$SIGNER_EXTENSION" = zip ]; then
    unzip -q "$SIGNER_ARCHIVE" -d "$STAGE/signer"
    SIGNER="$STAGE/signer/$SIGNER_RELEASE/rcodesign.exe"
  else
    tar -xzf "$SIGNER_ARCHIVE" -C "$STAGE/signer"
    SIGNER="$STAGE/signer/$SIGNER_RELEASE/rcodesign"
  fi
  touch "$STAGE/signing.toml"
  (cd "$ROOT" && "$SIGNER" --config-file "$STAGE/signing.toml" sign --timestamp-url none --entitlements-xml-file build/Packaging/MacOS/Entitlements.plist "$APPLICATION")
  test -s "$APPLICATION/Contents/_CodeSignature/CodeResources"
  if [ "$(uname -s)" = "Linux" ]; then
    command -v genisoimage >/dev/null 2>&1 || { echo "genisoimage is required to create a macOS disk image on Linux"; exit 1; }
    genisoimage -D -J -V Xeonstrap -no-pad -r -graft-points -o "$ISO" "Xeonstrap.app=$APPLICATION"
    mv "$ISO" "$DMG"
    [ -s "$DMG" ] || { echo "The macOS disk image is empty"; exit 1; }
    commit_artifact "$DMG" "$DMG_TARGET"
  fi
  TARBALL="$STAGE/Xeonstrap-$RID.tar"
  tar --sort=name --mtime="@${SOURCE_DATE_EPOCH:-0}" --owner=0 --group=0 --numeric-owner --exclude="Xeonstrap.app/Contents/MacOS/Xeonstrap" --exclude="Xeonstrap.app/Contents/MacOS/xeonstrap-virtualdisplay" -C "$STAGE" -cf "$TARBALL" Xeonstrap.app
  tar --mtime="@${SOURCE_DATE_EPOCH:-0}" --owner=0 --group=0 --numeric-owner --mode=0755 -C "$STAGE" -rf "$TARBALL" Xeonstrap.app/Contents/MacOS/Xeonstrap Xeonstrap.app/Contents/MacOS/xeonstrap-virtualdisplay
  gzip -n -f "$TARBALL"
  commit_artifact "$TARBALL.gz" "$TARBALL_TARGET"
  exit 0
fi

/usr/libexec/PlistBuddy -c "Set :CFBundleShortVersionString $VERSION" "$APPLICATION/Contents/Info.plist"
/usr/libexec/PlistBuddy -c "Set :CFBundleVersion $VERSION" "$APPLICATION/Contents/Info.plist"

if [ -n "${MACOS_SIGN_IDENTITY:-}" ]; then
  codesign --force --options runtime --sign "$MACOS_SIGN_IDENTITY" "$APPLICATION/Contents/MacOS/xeonstrap-virtualdisplay"
  codesign --force --deep --options runtime --entitlements "$ROOT/build/Packaging/MacOS/Entitlements.plist" --sign "$MACOS_SIGN_IDENTITY" "$APPLICATION"
else
  codesign --force --sign - "$APPLICATION/Contents/MacOS/xeonstrap-virtualdisplay"
  codesign --force --deep --entitlements "$ROOT/build/Packaging/MacOS/Entitlements.plist" --sign - "$APPLICATION"
fi

codesign --verify --deep --strict --verbose=2 "$APPLICATION"
ditto -c -k --keepParent "$APPLICATION" "$ARCHIVE"

if [ -n "${MACOS_NOTARY_PROFILE:-}" ] && [ -n "${MACOS_SIGN_IDENTITY:-}" ]; then
  xcrun notarytool submit "$ARCHIVE" --keychain-profile "$MACOS_NOTARY_PROFILE" --wait
  xcrun stapler staple "$APPLICATION"
  rm "$ARCHIVE"
  ditto -c -k --keepParent "$APPLICATION" "$ARCHIVE"
fi

create_disk_image() {
  local attempt
  for attempt in 1 2 3 4 5 6; do
    if hdiutil create -volname Xeonstrap -srcfolder "$APPLICATION" -ov -format UDZO "$DMG"; then
      return 0
    fi
    echo "hdiutil create failed on attempt $attempt, retrying"
    rm -f "$DMG"
    sleep $((attempt * 5))
  done
  echo "The disk image could not be created"
  return 1
}

create_disk_image

if [ -n "${MACOS_SIGN_IDENTITY:-}" ]; then
  codesign --force --sign "$MACOS_SIGN_IDENTITY" "$DMG"
fi

if [ -n "${MACOS_NOTARY_PROFILE:-}" ] && [ -n "${MACOS_SIGN_IDENTITY:-}" ]; then
  xcrun notarytool submit "$DMG" --keychain-profile "$MACOS_NOTARY_PROFILE" --wait
  xcrun stapler staple "$DMG"
  xcrun stapler validate "$APPLICATION"
  xcrun stapler validate "$DMG"
fi

commit_artifact "$ARCHIVE" "$ARCHIVE_TARGET"
commit_artifact "$DMG" "$DMG_TARGET"
