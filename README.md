# Floating Window Organizer

**Your favorite folders, always on your desktop.** iOS-style folder widgets for Windows 11.

<p align="center">
  <img src="screenshots/4-windows.png" alt="Four widgets on the desktop: small Downloads and dah.art widgets with +N overflow tiles, a large Games widget full of app icons, and a collapsed Documents widget" width="790">
</p>

I've always wanted a quick-access folder that's always there, in the same spot on my
desktop — so I built this small app (with the help of Claude). Each widget is a
frosted-glass card showing the contents of a folder you choose as a grid of icons. Pin your
Games, your Downloads, that project folder you open twenty times a day, and glance at it
right from the desktop instead of digging through File Explorer.

## Features

### 🗂️ Your folders, at a glance

- **Live folder view** — every widget mirrors a real folder, with real shell icons and
  thumbnails: app icons, image previews, the works.
- **As many as you want** — pin any number of folders, each placed wherever you drag it.
- **Three sizes** — Small, Medium or Large, per widget.
- **Never truncated silently** — when a folder holds more than fits, a **+N More** tile
  opens it in File Explorer.
- **Collapsible** — fold a widget down to just its title and item count when you don't
  need the contents.

### 🖥️ Part of the desktop, not in your way

- **Lives on the desktop layer** — above the wallpaper, below your app windows.
- **Survives Show desktop** — widgets stay put when you press **Win+D**.
- **Out of the taskbar and Alt+Tab** — no clutter where you switch apps.
- **Matches Windows** — the frosted glass follows your light or dark theme.

### 🎨 Make each one yours

- **Rename** any widget — or clear the name to go back to the folder's.
- **Tint the glass** any color, with a full-spectrum picker, opacity slider, hex entry and
  presets.
- **Always readable** — text flips between black and white to suit the color you pick.

<p align="center">
  <img src="screenshots/folder-right-click.png" alt="A widget's right-click menu: Refresh, Expand, Open in File Explorer, Change folder, Rename, Color, Size, Add widget, Remove widget, About, Exit" height="420">
  &nbsp;&nbsp;
  <img src="screenshots/colorpicker.png" alt="Widget color dialog with a saturation/brightness square, hue and opacity sliders, a preview swatch, hex field and presets" height="420">
</p>

### ⚙️ Set it and forget it

- **Runs from the tray** — even with no widgets. Left-click the icon to hide or show
  everything at once; right-click for the full menu.
- **Remembers everything** — folder, position, size, collapsed state, name and color all
  come back on the next launch.
- **Starts with Windows** — optional, chosen during install.
- **Never doubled up** — launching it again while it's running won't duplicate your widgets.

<p align="center">
  <img src="screenshots/trayicon.png" alt="The Floating Window Organizer tray icon with its tooltip" height="90">
  &nbsp;&nbsp;
  <img src="screenshots/tray-menu.png" alt="Tray menu with Add widget, Close widget, Hide all, Show all, About and Exit" height="265">
</p>

## Install

Download `FloatingWindowOrganizer-Setup-<version>.exe` from the
[Releases page](https://github.com/dah007/floating-window-organizer/releases) and run it.

- Installs for the current user only — no admin prompt.
- Bundles the .NET runtime; nothing else to install.
- Adds a Start menu shortcut and optionally starts the app when you sign in (toggle it later in **Settings > Apps > Startup**).
- Upgrading over an existing install closes the running copy first and restarts it after.
- Uninstalling stops the app but keeps your settings, so reinstalling restores your widgets.

On first run you're asked to pick a folder for your first widget.

## Using it

| Action | How |
| --- | --- |
| Move a widget | Drag anywhere on it. Its position is saved when you let go. |
| Open a file or folder | Double-click its icon |
| See items that don't fit | Double-click the **+N** tile (opens the folder in File Explorer) |
| Collapse to just the title row, or expand again | Double-click the title row, or right-click > **Collapse** / **Expand** |
| Re-read the folder after files change | Right-click > **Refresh** |
| Rename a widget | Right-click > **Rename**, type, press Enter (Esc cancels). Clear the name to go back to the folder's name. |
| Give a widget its own color | Right-click > **Color…**: pick a hue, shade and opacity (or type a hex value, or click a preset). **Default** returns to the Windows light/dark color. Text turns black or white automatically to stay readable. |
| See the version, or find the author's GitHub, LinkedIn and Buy Me a Coffee pages | Right-click a widget or the tray icon > **About** |
| Change folder, size (Small / Medium / Large), add, remove, exit | Right-click the widget |

Widgets don't appear in the taskbar or Alt+Tab.

### Tray icon

The app runs in the notification area (next to the clock), even with no widgets.

| Action | How |
| --- | --- |
| Hide or show every widget | Left-click the tray icon. Hidden widgets stay hidden after a restart. |
| Add widget, Close widget (pick one), Hide all, Show all, About, Exit | Right-click the tray icon |

## Settings

Saved as JSON at `%APPDATA%\FloatingWindowOrganizer\settings.json`: one entry per widget
with its folder, position, size, collapsed state, custom name and color, plus whether
widgets are currently hidden. Uninstalling keeps this file.

## Design

Colours, type, sizes, components and the app icon are documented in the
[Floating Window Organizer design system](https://claude.ai/artifact/F66iK2hjPvQfVSmr9UnrDT)
(Claude Design; ask the maintainer for access). The code is the source of truth: if the two
disagree, `WidgetPalette.cs`, `WidgetLayout.cs` and `FolderWindow.xaml` win.

## Development

### Requirements

- Windows 11 (22H2 or later)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Inno Setup 6.3+](https://jrsoftware.org/isinfo.php) — only to build the installer
  (`winget install JRSoftware.InnoSetup`)

### Solution layout

| Project | Target | Purpose |
| --- | --- | --- |
| `src/FloatingWindowOrganizer.App` | `net10.0-windows` (WPF) | Widget windows, desktop pinning, blur, shell icons, tray icon, color picker, About dialog |
| `src/FloatingWindowOrganizer.Core` | `net10.0` | Settings, widget sizes and layout, theme palette, color math, folder listing |
| `tests/FloatingWindowOrganizer.Tests` | `net10.0` (xUnit) | Unit tests for Core |
| `tests/FloatingWindowOrganizer.AppTests` | `net10.0-windows` (xUnit) | Unit tests for App-layer pure logic (`WidgetTheme`) |
| `tests/FloatingWindowOrganizer.TestSupport` | `net10.0` | Shared helpers for Smoke/E2E: launching an isolated app instance, locating the built exe |
| `tests/FloatingWindowOrganizer.Smoke` | `net10.0` (xUnit) | Launches the real built app; checks it starts/exits without crashing |
| `tests/FloatingWindowOrganizer.E2E` | `net10.0-windows` (xUnit, FlaUI) | Drives the real widget window and color picker via UI Automation |

### Build, test, run

```powershell
dotnet build
dotnet test --filter "Category!=Smoke&Category!=E2E"    # unit tests only (fast)
dotnet test tests\FloatingWindowOrganizer.Smoke         # launches the real app
dotnet test tests\FloatingWindowOrganizer.E2E           # drives the real app's UI
dotnet test                                             # everything
dotnet run --project src\FloatingWindowOrganizer.App
```

### Testing

Smoke and E2E tests launch the real `FloatingWindowOrganizer.exe` (the most recently built one
under `src\FloatingWindowOrganizer.App\bin`, or the path in `FWO_APP_EXE` if set), so build the
solution first. They need an interactive desktop session — they can't open windows from a
sandboxed or headless shell.

Each test gets its own isolated settings folder and single-instance mutex via two environment
variables the app honors only when set — `FWO_SETTINGS_DIR` (overrides where `settings.json` is
read/written) and `FWO_INSTANCE_ID` (suffixes the single-instance mutex name) — so tests never
touch your real `%APPDATA%\FloatingWindowOrganizer\settings.json` or collide with an instance of
the app you already have running. Both are no-ops for a normal launch.

E2E automates the widget window and color picker directly. Because widgets are desktop-owned
tool windows, the process never has a "main window" in the Win32 sense, so the tests locate the
widget by searching the desktop's top-level UI Automation children for the app's process ID.
Driving the system tray's notification-area menu is intentionally out of scope (too
inconsistent across machines/CI runners to automate reliably), as is the native folder-picker
dialog used by "Add widget" and "Change folder".

### Continuous integration

`.github/workflows/ci.yml` runs on every push to `main` and every pull request, with three
parallel jobs on `windows-latest`: unit tests, smoke tests, and E2E tests. Each uploads its
TRX results as a build artifact.

### Building the installer

```powershell
.\build\build-installer.ps1                  # version from Directory.Build.props
.\build\build-installer.ps1 -Version 0.2.0   # override the version
.\build\build-installer.ps1 -SkipTests       # skip the unit-test step
```

The script runs the unit tests, publishes a self-contained `win-x64` build to
`artifacts\publish\win-x64`, then compiles `installer\FloatingWindowOrganizer.iss`.
Output: `artifacts\installer\FloatingWindowOrganizer-Setup-<version>.exe`.

The version lives in one place: `<Version>` in `Directory.Build.props`.

### Releasing

Push a version tag and `.github/workflows/release.yml` builds the installer (stamped with the
tag's version) and attaches it to a GitHub Release with generated release notes:

```powershell
git tag v0.1.0
git push origin v0.1.0
```

## License

See [LICENSE](LICENSE).
