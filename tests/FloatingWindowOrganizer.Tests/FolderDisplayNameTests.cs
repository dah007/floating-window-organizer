using FloatingWindowOrganizer.Core.Services;

namespace FloatingWindowOrganizer.Tests;

public class FolderDisplayNameTests
{
    private static readonly string Root = Path.GetPathRoot(Path.GetTempPath())!;

    private static string Folder(params string[] parts) => Path.Combine(new[] { Root }.Concat(parts).ToArray());

    [Fact]
    public void For_ReturnsLastFolderName()
    {
        Assert.Equal("Documents", FolderDisplayName.For(Folder("Users", "me", "Documents")));
    }

    [Fact]
    public void For_IgnoresTrailingSeparator()
    {
        var path = Folder("Users", "me", "Documents") + Path.DirectorySeparatorChar;

        Assert.Equal("Documents", FolderDisplayName.For(path));
    }

    [Fact]
    public void For_DriveRoot_ReturnsWholePath()
    {
        Assert.Equal(Root, FolderDisplayName.For(Root));
    }

    [Fact]
    public void MenuLabels_UniqueNames_AreJustTheNames()
    {
        var labels = FolderDisplayName.MenuLabels(new[] { Folder("a", "Documents"), Folder("b", "Downloads") });

        Assert.Equal(new[] { "Documents", "Downloads" }, labels);
    }

    [Fact]
    public void MenuLabels_DuplicateNames_GetTheirPathAdded()
    {
        var first = Folder("Work", "Documents");
        var second = Folder("Home", "documents"); // names match regardless of case, as on Windows
        var other = Folder("Home", "Pictures");

        var labels = FolderDisplayName.MenuLabels(new[] { first, second, other });

        Assert.Equal(new[] { $"Documents ({first})", $"documents ({second})", "Pictures" }, labels);
    }

    [Fact]
    public void MenuLabels_KeepsInputOrder()
    {
        var labels = FolderDisplayName.MenuLabels(new[] { Folder("z", "Zeta"), Folder("a", "Alpha") });

        Assert.Equal(new[] { "Zeta", "Alpha" }, labels);
    }

    [Fact]
    public void MenuLabels_Empty_ReturnsEmpty()
    {
        Assert.Empty(FolderDisplayName.MenuLabels(Array.Empty<string>()));
    }
}
