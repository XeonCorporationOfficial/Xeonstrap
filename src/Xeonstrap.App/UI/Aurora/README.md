# Aurora Glass

The redesigned shell and visual language for Xeonstrap. Everything here is additive: the
legacy windows still build and run, and the new shell is opt-in with `-aurora` until the
page migration reaches parity.

## Opting in

```
Xeonstrap.exe -aurora
```

`-aurora` is an alias of the menu flag, so it opens the shell exactly like `-settings` opens
the old window. Without the flag nothing changes.

## Pieces

| File | Role |
| --- | --- |
| `AuroraTheme.xaml` | Keyed component styles: cards, hero, chips, search surface, four button kinds, rail items, typography. Merged from `App.xaml`, so any page can use them. |
| `AuroraNavigation.cs` | The rail model: five groups (Play, Tune, Flags, System, More) covering every page the legacy sidebar exposed. |
| `AuroraShell.xaml` | The window: grouped rail with search, command bar with back/forward, channel chip and the primary launch action, plus a `Frame` that hosts pages. |
| `AuroraHomePage.xaml` | New landing surface. |
| `AuroraMissingPage.xaml` | Fallback surface for a rail entry without a page yet. |
| `../Style/Aurora.xaml` | The theme itself (colour tokens). Selected by `Theme.Aurora`; new installs default to it and existing installs migrate once. |

## Design tokens

Everything resolves through the shared Xeon Aurora tokens in `UI/Style/RinUI.xaml`:

- ink `#07060F`, raised `#121024`, high `#1B1630`
- accent `#7C5CFF` → blue `#4F7CF6` gradient
- text `#F6F4FF` / `#C6BFDC` / `#928BAE`
- cards: 6% violet fill with a 13% violet stroke, radius 16
- controls: radius 10, pills: radius 999

Components never hard-code a colour; they reference `CardBackgroundFillColorDefaultBrush`,
`TextFillColor*Brush`, `AuroraAccentBrush` and friends, so Light and Custom themes keep working.

## Page migration status

Rewritten with Aurora XAML:

- Home (new), Help, Support, Shortcuts, News, History, Releases, Roblox News, Game, More,
  Deployment, Global, Mod generator
- Installer: Channel, Completion, Mods, Appearance

Chrome pass only (Aurora header and padding, legacy body):

- Settings (Channel), Sober, Overlays, Integrations, FastFlags, Extensions, Appearance, Mods,
  Installer/InstallPage, About, NVIDIA Settings

Padding pass only (Aurora page padding, legacy body and header):

- FastFlag Editor, Library, Manager (Downloads), NVIDIA Editor, Supporters, Translators,
  Licenses

## Shell behaviour

- Rail selection is grouped through a `ListCollectionView`; typing in the rail search filters
  entries live.
- `Ctrl+K` focuses the search, `Ctrl+S` saves, `Ctrl+1..9` jumps to a rail entry, `Esc` clears
  the filter.
- **Save** flushes settings, state and FastFlags and then runs the queued setting tasks
  (cursor, font, emoji and shortcut presets write files when executed - without this the
  legacy Save button was the only thing that applied them). The launch buttons and the close
  path also flush, so nothing is lost when the shell is used instead of the old window.
- The shell implements `INavigationWindow`, so legacy pages that call
  `((INavigationWindow)Window.GetWindow(this)).Navigate(typeof(...))` still work.
- The last visited rail entry is stored in `State.AuroraLastPage` and restored on open.

## Known gaps before flipping the default

1. The legacy Settings window still ships the command palette and the notification surface;
   the shell focuses rail search instead of a full palette and does not render app
   notifications yet.
2. Five settings pages still render their legacy body (see the list above).
3. Nothing here has been compiled on the machine that produced it: the first build should be
   treated as a verification step, with icon names the most likely source of errors.

## Rail parity check

All 27 rail entries resolve to an existing page type; `AuroraMissingPage` is only reached if a
page constructor throws. Verified by grepping every `PageType = typeof(...)` in
`AuroraNavigation.cs` against the page files in `UI/Elements/Settings/Pages`,
`UI/Elements/About/Pages` and `UI/Aurora`.
