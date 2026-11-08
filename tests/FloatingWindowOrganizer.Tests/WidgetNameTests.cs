using FloatingWindowOrganizer.Core.Models;
using FloatingWindowOrganizer.Core.Services;

namespace FloatingWindowOrganizer.Tests;

public class WidgetNameTests
{
    private static readonly string Documents = Path.Combine(Path.GetPathRoot(Path.GetTempPath())!, "Users", "me", "Documents");

    [Fact]
    public void For_WithoutCustomName_UsesFolderName()
    {
        Assert.Equal("Documents", WidgetName.For(new FolderWindowSettings { FolderPath = Documents }));
    }

    [Fact]
    public void For_WithCustomName_UsesIt()
    {
        var widget = new FolderWindowSettings { FolderPath = Documents, CustomName = "Paperwork" };

        Assert.Equal("Paperwork", WidgetName.For(widget));
    }

    [Fact]
    public void For_BlankCustomName_FallsBackToFolderName()
    {
        var widget = new FolderWindowSettings { FolderPath = Documents, CustomName = "   " };

        Assert.Equal("Documents", WidgetName.For(widget));
    }

    [Fact]
    public void Normalize_TrimsSpaces()
    {
        Assert.Equal("Paperwork", WidgetName.NormalizeCustomName("  Paperwork  ", Documents));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_Blank_MeansUseFolderName(string? typed)
    {
        Assert.Null(WidgetName.NormalizeCustomName(typed, Documents));
    }

    [Fact]
    public void Normalize_SameAsFolderName_MeansUseFolderName()
    {
        // So renaming the folder later still updates the title.
        Assert.Null(WidgetName.NormalizeCustomName("Documents", Documents));
    }

    [Fact]
    public void Normalize_DifferentCaseFromFolderName_IsKept()
    {
        Assert.Equal("DOCUMENTS", WidgetName.NormalizeCustomName("DOCUMENTS", Documents));
    }

    [Fact]
    public void Normalize_LongName_IsCappedAtMaxLength()
    {
        var name = WidgetName.NormalizeCustomName(new string('x', 100), Documents);

        Assert.Equal(WidgetName.MaxLength, name!.Length);
    }

    [Fact]
    public void MenuLabels_UseCustomNames_AndSeparateDuplicates()
    {
        var root = Path.GetPathRoot(Path.GetTempPath())!;
        var work = new FolderWindowSettings { FolderPath = Path.Combine(root, "Work"), CustomName = "Inbox" };
        var home = new FolderWindowSettings { FolderPath = Path.Combine(root, "Home"), CustomName = "Inbox" };
        var photos = new FolderWindowSettings { FolderPath = Path.Combine(root, "Photos") };

        var labels = WidgetName.MenuLabels(new[] { work, home, photos });

        Assert.Equal(new[] { $"Inbox ({work.FolderPath})", $"Inbox ({home.FolderPath})", "Photos" }, labels);
    }
}
