using System.Windows;
using FloatingWindowOrganizer.Core.Models;
using FloatingWindowOrganizer.Core.Services;
using Microsoft.Win32;

namespace FloatingWindowOrganizer.App;

public partial class App : Application
{
    private const double FirstWindowOffset = 100;

    private ISettingsStore _store = null!;
    private AppSettings _settings = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _store = new JsonSettingsStore(SettingsPaths.GetDefaultSettingsFilePath());
        _settings = _store.Load();

        if (_settings.Windows.Count == 0)
        {
            var folder = PromptForFolder();
            if (folder is null)
            {
                Shutdown();
                return;
            }

            _settings.Windows.Add(new FolderWindowSettings
            {
                FolderPath = folder,
                Left = FirstWindowOffset,
                Top = FirstWindowOffset,
            });
            SaveSettings();
        }

        foreach (var windowSettings in _settings.Windows)
        {
            new FolderWindow(windowSettings, SaveSettings).Show();
        }
    }

    /// <summary>Asks the user to choose a folder. Returns null if they cancel.</summary>
    internal static string? PromptForFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Choose a folder to float on your desktop",
        };

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    private void SaveSettings() => _store.Save(_settings);
}
