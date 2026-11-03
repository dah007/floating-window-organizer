using FloatingWindowOrganizer.Core.Models;

namespace FloatingWindowOrganizer.Core.Services;

public static class FolderContents
{
    /// <summary>
    /// Lists the folder's immediate children: sub-folders first, then files, each sorted by name.
    /// Hidden and system items are skipped.
    /// </summary>
    /// <exception cref="DirectoryNotFoundException">The folder doesn't exist.</exception>
    public static IReadOnlyList<FolderEntry> List(string folderPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folderPath);

        var directory = new DirectoryInfo(folderPath);
        if (!directory.Exists)
        {
            throw new DirectoryNotFoundException($"Folder not found: {folderPath}");
        }

        return directory
            .EnumerateFileSystemInfos()
            .Where(item => (item.Attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0)
            .Select(item => new FolderEntry(item.Name, item.FullName, item is DirectoryInfo))
            .OrderByDescending(entry => entry.IsDirectory)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
