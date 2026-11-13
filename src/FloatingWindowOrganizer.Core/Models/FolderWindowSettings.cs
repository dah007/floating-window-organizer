namespace FloatingWindowOrganizer.Core.Models;

/// <summary>
/// Persisted state for a single folder widget.
/// </summary>
public sealed class FolderWindowSettings
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The folder whose contents the widget displays.</summary>
    public string FolderPath { get; set; } = string.Empty;

    public double Left { get; set; }

    public double Top { get; set; }

    public WidgetSize Size { get; set; } = WidgetSize.Medium;

    /// <summary>True when the widget is collapsed to just its title row.</summary>
    public bool IsCollapsed { get; set; }

    /// <summary>Name shown instead of the folder's name; null means use the folder's name.</summary>
    public string? CustomName { get; set; }

    /// <summary>
    /// Custom background as "#AARRGGBB" (alpha = opacity of the glass tint); null means follow the
    /// Windows light/dark theme. An unreadable value is treated as null.
    /// </summary>
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// True (the default, also for settings files saved before this existed) when the glossy glass
    /// sheen is drawn over the widget. The sheen never takes mouse input.
    /// </summary>
    public bool GlossEnabled { get; set; } = true;
}
