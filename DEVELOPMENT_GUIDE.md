# Development

[< Back to README.md](README.md)

## Requirements

- Windows 11 (22H2 or later)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Inno Setup 6.3+](https://jrsoftware.org/isinfo.php) — only to build the installer
  (`winget install JRSoftware.InnoSetup`)

## Solution layout

| Project | Target | Purpose |
| --- | --- | --- |
| `src/FloatingWindowOrganizer.App` | `net10.0-windows` (WPF) | Widget windows, desktop pinning, glossy glass, wallpaper color sampling, shell icons, tray icon, color picker, About dialog |
| `src/FloatingWindowOrganizer.Core` | `net10.0` | Settings, widget sizes and layout, theme and adaptive glass palette, color math, folder listing |
| `tests/FloatingWindowOrganizer.Tests` | `net10.0` (xUnit) | Unit tests for Core |
| `tests/FloatingWindowOrganizer.AppTests` | `net10.0-windows` (xUnit) | Unit tests for App-layer pure logic (`WidgetTheme`) |
| `tests/FloatingWindowOrganizer.TestSupport` | `net10.0` | Shared helpers for Smoke/E2E: launching an isolated app instance, locating the built exe |
| `tests/FloatingWindowOrganizer.Smoke` | `net10.0` (xUnit) | Launches the real built app; checks it starts/exits without crashing |
| `tests/FloatingWindowOrganizer.E2E` | `net10.0-windows` (xUnit, FlaUI) | Drives the real widget window and color picker via UI Automation |

## Build, test, run

```powershell
dotnet build
dotnet test --filter "Category!=Smoke&Category!=E2E"    # unit tests only (fast)
dotnet test tests\FloatingWindowOrganizer.Smoke         # launches the real app
dotnet test tests\FloatingWindowOrganizer.E2E           # drives the real app's UI
dotnet test                                             # everything
dotnet run --project src\FloatingWindowOrganizer.App
```

## Testing

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

## Continuous integration

`.github/workflows/ci.yml` runs on every push to `main` and every pull request, with three
parallel jobs on `windows-latest`: unit tests, smoke tests, and E2E tests. Each uploads its
TRX results as a build artifact.

## Building the installer

```powershell
.\build\build-installer.ps1                  # version from Directory.Build.props
.\build\build-installer.ps1 -Version 0.2.0   # override the version
.\build\build-installer.ps1 -SkipTests       # skip the unit-test step
```

The script runs the unit tests, publishes a self-contained `win-x64` build to
`artifacts\publish\win-x64`, then compiles `installer\FloatingWindowOrganizer.iss`.
Output: `artifacts\installer\FloatingWindowOrganizer-Setup-<version>.exe`.

The version lives in one place: `<Version>` in `Directory.Build.props`.

## Releasing

Push a version tag and `.github/workflows/release.yml` builds the installer (stamped with the
tag's version) and attaches it to a GitHub Release with generated release notes:

```powershell
git tag v0.1.0
git push origin v0.1.0
```
