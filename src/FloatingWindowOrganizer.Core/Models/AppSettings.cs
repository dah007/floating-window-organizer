namespace FloatingWindowOrganizer.Core.Models;

/// <summary>
/// Root settings document saved to disk.
/// </summary>
public sealed class AppSettings
{
    public const int CurrentSchemaVersion = 1;

    /// <summary>Lets future versions migrate older settings files.</summary>
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public List<FolderWindowSettings> Windows { get; set; } = [];

    /// <summary>True when the user chose "Hide all" from the tray; widgets stay hidden across restarts.</summary>
    public bool WidgetsHidden { get; set; }
}
