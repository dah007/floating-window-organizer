using FloatingWindowOrganizer.Core.Models;
using FloatingWindowOrganizer.Core.Services;

namespace FloatingWindowOrganizer.Tests;

public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    [Fact]
    public void Load_WhenFileMissing_ReturnsEmptyDefaults()
    {
        var store = new JsonSettingsStore(_temp.Combine("settings.json"));

        var settings = store.Load();

        Assert.Empty(settings.Windows);
        Assert.Equal(AppSettings.CurrentSchemaVersion, settings.SchemaVersion);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsAllWindowValues()
    {
        var store = new JsonSettingsStore(_temp.Combine("settings.json"));
        var original = new FolderWindowSettings
        {
            FolderPath = @"C:\Users\me\Documents",
            Left = 123.5,
            Top = -40,
            Size = WidgetSize.Large,
            IsCollapsed = true,
        };
        store.Save(new AppSettings { Windows = [original] });

        var loaded = Assert.Single(store.Load().Windows);

        Assert.Equal(original.Id, loaded.Id);
        Assert.Equal(original.FolderPath, loaded.FolderPath);
        Assert.Equal(original.Left, loaded.Left);
        Assert.Equal(original.Top, loaded.Top);
        Assert.Equal(original.Size, loaded.Size);
        Assert.Equal(original.IsCollapsed, loaded.IsCollapsed);
    }

    [Fact]
    public void NewWidget_StartsExpanded()
    {
        Assert.False(new FolderWindowSettings().IsCollapsed);
    }

    [Fact]
    public void NewWidget_DefaultsToMediumSize()
    {
        Assert.Equal(WidgetSize.Medium, new FolderWindowSettings().Size);
    }

    [Fact]
    public void Save_WritesWidgetSizeAsReadableText()
    {
        var path = _temp.Combine("settings.json");
        var store = new JsonSettingsStore(path);

        store.Save(new AppSettings { Windows = [new FolderWindowSettings { Size = WidgetSize.Small }] });

        Assert.Contains("\"Small\"", File.ReadAllText(path));
    }

    [Fact]
    public void Load_IgnoresWidthAndHeightFromOlderSettingsFiles()
    {
        var path = _temp.Combine("settings.json");
        File.WriteAllText(path, """
            { "SchemaVersion": 1, "Windows": [ { "FolderPath": "C:\\Temp", "Left": 10, "Top": 20, "Width": 300, "Height": 400 } ] }
            """);

        var loaded = Assert.Single(new JsonSettingsStore(path).Load().Windows);

        Assert.Equal(@"C:\Temp", loaded.FolderPath);
        Assert.Equal(10, loaded.Left);
        Assert.Equal(WidgetSize.Medium, loaded.Size);
        Assert.False(loaded.IsCollapsed);
    }

    [Fact]
    public void Load_WhenSizeIsUnknown_TreatsFileAsCorrupt()
    {
        var path = _temp.Combine("settings.json");
        File.WriteAllText(path, """{ "Windows": [ { "Size": "Gigantic" } ] }""");
        var store = new JsonSettingsStore(path);

        var settings = store.Load();

        Assert.Empty(settings.Windows);
        Assert.True(File.Exists(store.CorruptBackupPath));
    }

    [Fact]
    public void Save_CreatesMissingDirectory()
    {
        var path = _temp.Combine("nested", "deeper", "settings.json");
        var store = new JsonSettingsStore(path);

        store.Save(new AppSettings());

        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Save_OverwritesExistingFileAndLeavesNoTempFile()
    {
        var path = _temp.Combine("settings.json");
        var store = new JsonSettingsStore(path);
        store.Save(new AppSettings { Windows = [new FolderWindowSettings(), new FolderWindowSettings()] });

        store.Save(new AppSettings { Windows = [new FolderWindowSettings()] });

        Assert.Single(store.Load().Windows);
        Assert.False(File.Exists(path + ".tmp"));
    }

    [Fact]
    public void Load_WhenFileIsCorrupt_ReturnsDefaultsAndKeepsBackup()
    {
        var path = _temp.Combine("settings.json");
        File.WriteAllText(path, "{ this is not json");
        var store = new JsonSettingsStore(path);

        var settings = store.Load();

        Assert.Empty(settings.Windows);
        Assert.False(File.Exists(path));
        Assert.Equal("{ this is not json", File.ReadAllText(store.CorruptBackupPath));
    }

    [Fact]
    public void Load_WhenWindowsIsNull_ReturnsEmptyList()
    {
        var path = _temp.Combine("settings.json");
        File.WriteAllText(path, """{ "SchemaVersion": 1, "Windows": null }""");

        var settings = new JsonSettingsStore(path).Load();

        Assert.NotNull(settings.Windows);
        Assert.Empty(settings.Windows);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsBlankPath(string path)
    {
        Assert.ThrowsAny<ArgumentException>(() => new JsonSettingsStore(path));
    }

    [Fact]
    public void Save_RejectsNullSettings()
    {
        var store = new JsonSettingsStore(_temp.Combine("settings.json"));

        Assert.Throws<ArgumentNullException>(() => store.Save(null!));
    }
}
