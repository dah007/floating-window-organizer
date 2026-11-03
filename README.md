# Floating Window Organizer

A Windows desktop app that shows one or more "floating" folder windows on your desktop.
Each window displays the contents of a folder you choose, can be dragged anywhere, and
remembers its position once you finish dragging. The UI follows the current Windows
theme (light/dark and accent colour).

## Requirements

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Solution layout

| Project | Target | Purpose |
|---|---|---|
| `src/FloatingWindowOrganizer.App` | `net10.0-windows` (WPF) | The floating windows (UI only) |
| `src/FloatingWindowOrganizer.Core` | `net10.0` | Settings, persistence, window placement, folder listing |
| `tests/FloatingWindowOrganizer.Tests` | `net10.0` (xUnit) | Unit tests for Core |

## Build, test, run

```powershell
dotnet build
dotnet test
dotnet run --project src/FloatingWindowOrganizer.App
```

## Settings

Saved as JSON at `%APPDATA%\FloatingWindowOrganizer\settings.json`. Each entry holds a
folder path plus the window's position and size. On first run (no saved windows) you are
asked to pick a folder.
