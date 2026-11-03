using System.IO;
using System.Windows;
using FloatingWindowOrganizer.Core.Models;
using FloatingWindowOrganizer.Core.Services;
using Microsoft.Win32;

namespace FloatingWindowOrganizer.App;

public partial class App : Application
{
    private const double NewWidgetOffset = 100;
    private const double NewWidgetCascade = 30;

    // Per-user ("Local\") so each signed-in user can run their own copy.
    private const string SingleInstanceMutexName = @"Local\FloatingWindowOrganizer.SingleInstance";

    private Mutex? _singleInstanceMutex;

    private ISettingsStore _store = null!;
    private AppSettings _settings = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Started at sign-in and again from the Start menu would otherwise show every widget twice.
        _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
            Shutdown();
            return;
        }

        WidgetTheme.Apply(Resources);
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        _store = new JsonSettingsStore(SettingsPaths.GetDefaultSettingsFilePath());
        _settings = _store.Load();

        if (_settings.Windows.Count == 0)
        {
            if (AddWidget() is null)
            {
                Shutdown();
            }

            return;
        }

        foreach (var widgetSettings in _settings.Windows)
        {
            ShowWidget(widgetSettings);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    /// <summary>Asks the user to choose a folder. Returns null if they cancel.</summary>
    internal static string? PromptForFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Choose a folder for the widget",
        };

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    /// <summary>Prompts for a folder and adds a new widget for it. Returns null if cancelled.</summary>
    internal FolderWindow? AddWidget()
    {
        var folder = PromptForFolder();
        if (folder is null)
        {
            return null;
        }

        var cascade = NewWidgetCascade * _settings.Windows.Count;
        var widgetSettings = new FolderWindowSettings
        {
            FolderPath = folder,
            Left = NewWidgetOffset + cascade,
            Top = NewWidgetOffset + cascade,
            Size = WidgetSize.Medium,
        };
        _settings.Windows.Add(widgetSettings);
        SaveSettings();

        return ShowWidget(widgetSettings);
    }

    /// <summary>Removes the widget from settings and closes it. Closing the last one exits the app.</summary>
    internal void RemoveWidget(FolderWindow widget)
    {
        _settings.Windows.Remove(widget.Settings);
        SaveSettings();
        widget.Close();
    }

    internal void SaveSettings()
    {
        try
        {
            _store.Save(_settings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                $"Couldn't save widget settings.\n\n{ex.Message}",
                "Floating Window Organizer",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private FolderWindow ShowWidget(FolderWindowSettings widgetSettings)
    {
        var widget = new FolderWindow(widgetSettings, this);
        widget.Show();
        return widget;
    }

    private void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        // Light/dark switches arrive as General; SystemEvents may raise this off the UI thread.
        if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.Color or UserPreferenceCategory.VisualStyle)
        {
            Dispatcher.InvokeAsync(() => WidgetTheme.Apply(Resources));
        }
    }
}
