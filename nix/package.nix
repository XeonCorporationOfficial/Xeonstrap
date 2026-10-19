{
  lib,
  stdenv,
  buildDotnetModule,
  dotnetCorePackages,
  fetchFromGitHub,
  rustPlatform,
  autoPatchelfHook,
  nix-update-script,
  alsa-lib,
  dbus,
  fontconfig,
  freetype,
  glib,
  gst_all_1,
  icu,
  libGL,
  libICE,
  libSM,
  libX11,
  libXcomposite,
  libXcursor,
  libXdamage,
  libXext,
  libXfixes,
  libXi,
  libXrandr,
  libXrender,
  libxkbcommon,
  libdrm,
  mesa,
  openssl,
  vulkan-loader,
  wayland,
  zlib,
}:

let
  runtimeLibraries = [
    alsa-lib
    dbus
    fontconfig
    freetype
    glib
    gst_all_1.gstreamer
    gst_all_1.gst-plugins-base
    icu
    libGL
    libICE
    libSM
    libX11
    libXcomposite
    libXcursor
    libXdamage
    libXext
    libXfixes
    libXi
    libXrandr
    libXrender
    libxkbcommon
    libdrm
    mesa
    openssl
    vulkan-loader
    wayland
    zlib
  ];
in
buildDotnetModule (finalAttrs: {
  pname = "xeonstrap";
  version = "1.1.3.5";

  src = fetchFromGitHub {
    owner = "XeonCorporationOfficial";
    repo = "Xeonstrap";
    tag = "v${finalAttrs.version}";
    fetchSubmodules = true;
    hash = "sha256-xLKSOLN0qXCjZWRNSILExvy4RdFxtlK+MOmJmxdf4o8=";
  };

  projectFile = "src/Xeonstrap.Cross/Xeonstrap.Cross.csproj";
  nugetDeps = ./deps.json;

  dotnet-sdk = dotnetCorePackages.sdk_10_0;
  dotnet-runtime = null;
  selfContainedBuild = true;

  executables = [ "Xeonstrap" ];

  nativeBuildInputs = [ autoPatchelfHook ];
  buildInputs = [ (lib.getLib stdenv.cc.cc) ] ++ runtimeLibraries;
  runtimeDeps = runtimeLibraries;
  autoPatchelfIgnoreMissingDeps = [ "*" ];

  preConfigure = ''
    dotnetFlags+=("-p:XeonstrapLinuxPackagingRoot=$PWD/.nix-artifacts/")
  '';

  dotnetBuildFlags = [ "-p:XeonImageLibrary=${finalAttrs.passthru.xeonimage}/lib/libxeonimage.so" ];
  dotnetInstallFlags = [
    "-p:PublishSingleFile=false"
    "-p:IncludeAllContentForSelfExtract=false"
    "-p:DebugType=None"
    "-p:DebugSymbols=false"
    "-p:XeonImageLibrary=${finalAttrs.passthru.xeonimage}/lib/libxeonimage.so"
  ];

  env = {
    XEONSTRAP_XEONIMAGE_NO_ZIG = "1";
    XEONSTRAP_EDITOR_COMPILER = "${stdenv.cc}/bin/cc";
  };

  postInstall = ''
    for library in uc i18n data; do
      if [ -e $out/lib/xeonstrap/libicu$library.so.72.1.0.3 ]; then
        ln -s libicu$library.so.72.1.0.3 $out/lib/xeonstrap/libicu$library.so.72
      fi
    done
    install -Dm644 build/Packaging/Linux/xeonstrap.desktop \
      $out/share/applications/io.github.XeonCorporationOfficial.Xeonstrap.desktop
    install -Dm644 src/Xeonstrap.App/Xeonstrap.png \
      $out/share/icons/hicolor/256x256/apps/io.github.XeonCorporationOfficial.Xeonstrap.png
    install -Dm644 build/Packaging/Linux/io.github.XeonCorporationOfficial.Xeonstrap.metainfo.xml \
      $out/share/metainfo/io.github.XeonCorporationOfficial.Xeonstrap.metainfo.xml
  '';

  postFixup = ''
    ln -s Xeonstrap $out/bin/xeonstrap
  '';

  passthru = {
    xeonimage = rustPlatform.buildRustPackage {
      pname = "xeonimage";
      inherit (finalAttrs) version src;
      cargoRoot = "native/xeonimage";
      buildAndTestSubdir = "native/xeonimage";
      cargoHash = "sha256-25ly3BBUjnx7nzhCbZi0GkHVuWYEtj+PG1Y1EefzZqY=";
      doCheck = false;
    };
    updateScript = nix-update-script { };
  };

  meta = {
    description = "Roblox bootstrapper that launches Roblox through Sober on Linux";
    homepage = "https://github.com/XeonCorporationOfficial/Xeonstrap";
    changelog = "https://github.com/XeonCorporationOfficial/Xeonstrap/releases/tag/v${finalAttrs.version}";
    license = lib.licenses.mit;
    mainProgram = "xeonstrap";
    platforms = [
      "x86_64-linux"
      "aarch64-linux"
    ];
  };
})
