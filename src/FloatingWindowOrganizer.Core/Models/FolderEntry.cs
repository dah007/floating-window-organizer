namespace FloatingWindowOrganizer.Core.Models;

/// <summary>
/// A file or sub-folder shown inside a floating folder window.
/// </summary>
public sealed record FolderEntry(string Name, string FullPath, bool IsDirectory);
