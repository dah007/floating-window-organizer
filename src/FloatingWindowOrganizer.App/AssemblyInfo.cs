using System.Runtime.CompilerServices;
using System.Windows;

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,
    ResourceDictionaryLocation.SourceAssembly)]

// So AppTests can exercise internal-only App-layer logic (e.g. WidgetTheme) directly.
[assembly: InternalsVisibleTo("FloatingWindowOrganizer.AppTests")]
