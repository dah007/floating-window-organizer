using FloatingWindowOrganizer.Core.Models;

namespace FloatingWindowOrganizer.Core.Services;

public static class WidgetName
{
    public const int MaxLength = 64;

    /// <summary>The name shown in the widget's title row: the custom name if set, else the folder's name.</summary>
    public static string For(FolderWindowSettings widget)
    {
        ArgumentNullException.ThrowIfNull(widget);

        return string.IsNullOrWhiteSpace(widget.CustomName)
            ? FolderDisplayName.For(widget.FolderPath)
            : widget.CustomName.Trim();
    }

    /// <summary>
    /// Turns what the user typed into the value to save: trimmed and capped at <see cref="MaxLength"/>.
    /// Returns null (use the folder's name) when it is blank or the same as the folder's name.
    /// </summary>
    public static string? NormalizeCustomName(string? typed, string folderPath)
    {
        var name = typed?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        if (name.Length > MaxLength)
        {
            name = name[..MaxLength].TrimEnd();
        }

        return string.Equals(name, FolderDisplayName.For(folderPath), StringComparison.Ordinal) ? null : name;
    }

    /// <summary>Tray menu labels for the widgets, telling apart widgets that share a name.</summary>
    public static IReadOnlyList<string> MenuLabels(IReadOnlyList<FolderWindowSettings> widgets)
    {
        ArgumentNullException.ThrowIfNull(widgets);

        return FolderDisplayName.MenuLabels(
            widgets.Select(For).ToList(),
            widgets.Select(widget => widget.FolderPath).ToList());
    }
}
