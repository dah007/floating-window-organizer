namespace FloatingWindowOrganizer.Core.Services;

public static class FolderDisplayName
{
    /// <summary>
    /// The name shown for a widget's folder: its last path segment, or the whole path for a
    /// drive root (which has no name of its own).
    /// </summary>
    public static string For(string folderPath)
    {
        ArgumentNullException.ThrowIfNull(folderPath);

        var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(folderPath));
        return string.IsNullOrEmpty(name) ? folderPath : name;
    }

    /// <summary>
    /// Menu labels for a list of folders, in the same order. Folders that share a name
    /// (e.g. two "Documents") get their full path added so they can be told apart.
    /// </summary>
    public static IReadOnlyList<string> MenuLabels(IReadOnlyList<string> folderPaths)
    {
        ArgumentNullException.ThrowIfNull(folderPaths);

        return MenuLabels(folderPaths.Select(For).ToList(), folderPaths);
    }

    /// <summary>
    /// Menu labels from display names and their folder paths (same order). Names that occur more
    /// than once get the folder path added.
    /// </summary>
    public static IReadOnlyList<string> MenuLabels(IReadOnlyList<string> names, IReadOnlyList<string> folderPaths)
    {
        ArgumentNullException.ThrowIfNull(names);
        ArgumentNullException.ThrowIfNull(folderPaths);
        if (names.Count != folderPaths.Count)
        {
            throw new ArgumentException("Each name needs a folder path.", nameof(folderPaths));
        }

        var duplicated = names
            .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return names
            .Select((name, index) => duplicated.Contains(name) ? $"{name} ({folderPaths[index]})" : name)
            .ToList();
    }
}
