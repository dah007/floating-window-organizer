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

    // Per-user ("Local\") so each signed-in user can run their own copy. Suffixed with
    // FWO_INSTANCE_ID when set, so automated tests never collide with a real running instance.
    private static string SingleInstanceMutexName =>
        Environment.GetEnvironmentVariable("FWO_INSTANCE_ID") is { Length: > 0 } id
            ? @$"Local\FloatingWindowOrganizer.SingleInstance.{id}"
            : @"Local\FloatingWindowOrganizer.SingleInstance";

    private Mutex? _singleInstanceMutex;

    private readonly List<FolderWindow> _widgets = [];

    private ISettingsStore _store = null!;
    private AppSettings _settings = null!;
    private TrayIcon? _tray;
    private AboutWindow? _about;

    /// <summary>Every open widget, in the order they were created.</summary>
    internal IReadOnlyList<FolderWindow> Widgets => _widgets;

    internal bool WidgetsHidden => _settings.WidgetsHidden;

    /// <summary>
    /// Average colour of the desktop wallpaper (0xFFRRGGBB), or null when it can't be read. Widgets with a
    /// custom colour use it to pick their text colour and glass opacity.
    /// </summary>
    internal uint? WallpaperColor { get; private set; }

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
        WallpaperColor = Wallpaper.AverageColor();
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        var settingsPath = SettingsPaths.GetDefaultSettingsFilePath();
        var isFirstRun = !File.Exists(settingsPath);
        _store = new JsonSettingsStore(settingsPath);
        _settings = _store.Load();

        // The app now lives in the tray, so it keeps running even with no widgets.
        _tray = new TrayIcon(this);

        foreach (var widgetSettings in _settings.Windows)
        {
            CreateWidget(widgetSettings);
        }

        // Only the very first run asks for a folder; someone who removed every widget isn't nagged at each sign-in.
        if (isFirstRun && _settings.Windows.Count == 0)
        {
            AddWidget();
        }

        if (_widgets.Count == 0)
        {
            _tray.ShowNotification("Running in the notification area. Right-click the icon to add a widget.");
        }
        else if (_settings.WidgetsHidden)
        {
            _tray.ShowNotification("Your widgets are hidden. Click the icon to show them.");
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _tray?.Dispose();
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

        // A new widget added while the others are hidden would be invisible; bring them all back.
        if (_settings.WidgetsHidden)
        {
            ShowAllWidgets();
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

        return CreateWidget(widgetSettings);
    }

    /// <summary>
    /// Asks, then removes the widget from settings and closes it. The app keeps running in the tray.
    /// </summary>
    internal void ConfirmAndRemoveWidget(FolderWindow widget)
    {
        var answer = MessageBox.Show(
            $"Remove the \"{WidgetName.For(widget.Settings)}\" widget? The folder itself isn't touched.",
            "Remove widget",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        _settings.Windows.Remove(widget.Settings);
        _widgets.Remove(widget);
        SaveSettings();
        widget.Close();
    }

    /// <summary>Shows the About dialog, or brings it forward if it's already open.</summary>
    internal void ShowAbout()
    {
        if (_about is not null)
        {
            _about.Activate();
            return;
        }

        _about = new AboutWindow();
        _about.Closed += (_, _) => _about = null;
        _about.ShowDialog();
    }

    internal void HideAllWidgets()
    {
        _settings.WidgetsHidden = true;
        SaveSettings();
        foreach (var widget in _widgets)
        {
            widget.Hide();
        }
    }

    internal void ShowAllWidgets()
    {
        _settings.WidgetsHidden = false;
        SaveSettings();
        foreach (var widget in _widgets)
        {
            widget.ShowOnDesktop();
        }
    }

    internal void ToggleWidgetsHidden()
    {
        if (_settings.WidgetsHidden)
        {
            ShowAllWidgets();
        }
        else if (_widgets.Count > 0)
        {
            HideAllWidgets();
        }
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

    /// <summary>Creates a widget window; it's shown unless the user has hidden all widgets.</summary>
    private FolderWindow CreateWidget(FolderWindowSettings widgetSettings)
    {
        var widget = new FolderWindow(widgetSettings, this);
        _widgets.Add(widget);
        if (!_settings.WidgetsHidden)
        {
            widget.Show();
        }

        return widget;
    }

    private void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        // Light/dark switches arrive as General; SystemEvents may raise this off the UI thread.
        if (e.Category is UserPreferenceCategory.General or UserPreferenceCategory.Color or UserPreferenceCategory.VisualStyle)
        {
            Dispatcher.InvokeAsync(() => WidgetTheme.Apply(Resources));
        }

        // A new wallpaper (or solid desktop colour) changes what custom-coloured glass sits on.
        if (e.Category is UserPreferenceCategory.Desktop or UserPreferenceCategory.Color)
        {
            Dispatcher.InvokeAsync(() =>
            {
                WallpaperColor = Wallpaper.AverageColor();
                foreach (var widget in _widgets)
                {
                    widget.RefreshColor();
                }
            });
        }
    }
}
