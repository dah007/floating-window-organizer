# Floating Window Organizer

iOS-style folder widgets for the Windows desktop. Each widget is a frosted-glass card
showing the contents of a folder you choose as a grid of icons. Widgets live on the
desktop layer: they sit above the wallpaper and below your app windows, and stay
visible when you press Win+D (Show desktop). Light and dark mode follow Windows.

## Using it

| Action | How |
|---|---|
| Move a widget | Drag anywhere on it. Its position is saved when you let go. |
| Open a file or folder | Double-click its icon |
| See items that don't fit | Double-click the **+N** tile (opens the folder in File Explorer) |
| Collapse to just the title row, or expand again | Double-click the title row, or right-click > **Collapse** / **Expand** |
| Re-read the folder after files change | Right-click > **Refresh** |
| Change folder, size (Small / Medium / Large), add, remove, exit | Right-click the widget |

Widgets don't appear in the taskbar or Alt+Tab.

### Tray icon

The app runs in the notification area (next to the clock), even with no widgets.

| Action | How |
|---|---|
| Hide or show every widget | Left-click the tray icon. Hidden widgets stay hidden after a restart. |
| Add widget, Close widget (pick one), Hide all, Show all, Exit | Right-click the tray icon |

## Requirements

- Windows 11 (22H2 or later)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Solution layout

| Project | Target | Purpose |
|---|---|---|
| `src/FloatingWindowOrganizer.App` | `net10.0-windows` (WPF) | Widget windows, desktop pinning, blur, shell icons |
| `src/FloatingWindowOrganizer.Core` | `net10.0` | Settings, widget sizes and layout, theme palette, folder listing |
| `tests/FloatingWindowOrganizer.Tests` | `net10.0` (xUnit) | Unit tests for Core |

## Build, test, run

```powershell
dotnet build
dotnet test
dotnet run --project src/FloatingWindowOrganizer.App
```

## Building the installer

Requires [Inno Setup 6.3+](https://jrsoftware.org/isinfo.php) (`winget install JRSoftware.InnoSetup`).

```powershell
.\build\build-installer.ps1              # version from Directory.Build.props
.\build\build-installer.ps1 -Version 0.2.0
```

Output: `artifacts\installer\FloatingWindowOrganizer-Setup-<version>.exe`. It installs for the
current user only (no admin prompt), bundles the .NET runtime, adds a Start menu shortcut, and
offers to start the app when you sign in.

### Releasing

Push a version tag and GitHub Actions builds the installer and attaches it to a GitHub Release:

```powershell
git tag v0.1.0
git push origin v0.1.0
```

## Settings

Saved as JSON at `%APPDATA%\FloatingWindowOrganizer\settings.json`: one entry per widget
with its folder, position, size and whether it's collapsed. On first run (no widgets yet) you're asked to pick a folder.

Uninstalling keeps this file, so reinstalling restores your widgets.
