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
}
