using FloatingWindowOrganizer.Core.Services;
using FloatingWindowOrganizer.TestSupport;

namespace FloatingWindowOrganizer.Tests;

public sealed class FolderContentsTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void List_ReturnsFoldersFirstThenFiles_EachSortedByName()
    {
        File.WriteAllText(_temp.Combine("b.txt"), "");
        File.WriteAllText(_temp.Combine("A.txt"), "");
        Directory.CreateDirectory(_temp.Combine("zeta"));
        Directory.CreateDirectory(_temp.Combine("Alpha"));

        var entries = FolderContents.List(_temp.Path);

        Assert.Equal(new[] { "Alpha", "zeta", "A.txt", "b.txt" }, entries.Select(e => e.Name));
        Assert.Equal(new[] { true, true, false, false }, entries.Select(e => e.IsDirectory));
    }

    [Fact]
    public void List_ReturnsFullPaths()
    {
        File.WriteAllText(_temp.Combine("file.txt"), "");

        var entry = Assert.Single(FolderContents.List(_temp.Path));

        Assert.Equal(_temp.Combine("file.txt"), entry.FullPath);
    }

    [Fact]
    public void List_EmptyFolder_ReturnsEmpty()
    {
        Assert.Empty(FolderContents.List(_temp.Path));
    }

    [Fact]
    public void List_MissingFolder_Throws()
    {
        Assert.Throws<DirectoryNotFoundException>(() => FolderContents.List(_temp.Combine("does-not-exist")));
    }

    [Fact]
    public void List_SkipsHiddenItems()
    {
        // Leading dot = hidden on Linux/macOS; the attribute = hidden on Windows.
        var hidden = _temp.Combine(".hidden.txt");
        File.WriteAllText(hidden, "");
        File.SetAttributes(hidden, File.GetAttributes(hidden) | FileAttributes.Hidden);
        File.WriteAllText(_temp.Combine("visible.txt"), "");

        var entries = FolderContents.List(_temp.Path);

        Assert.Equal(new[] { "visible.txt" }, entries.Select(e => e.Name));
    }
}
