using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using FloatingWindowOrganizer.Core.Models;
using FloatingWindowOrganizer.Core.Services;

namespace FloatingWindowOrganizer.App;

public partial class FolderWindow : Window
{
    private readonly FolderWindowSettings _settings;
    private readonly Action _saveSettings;

    public FolderWindow(FolderWindowSettings settings, Action saveSettings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(saveSettings);

        InitializeComponent();

        _settings = settings;
        _saveSettings = saveSettings;

        Width = settings.Width;
        Height = settings.Height;

        var desktop = new ScreenRect(
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth,
            SystemParameters.VirtualScreenHeight);
        (Left, Top) = WindowPlacement.EnsureVisible(settings.Left, settings.Top, Width, Height, desktop);

        LoadFolder();
    }

    private void LoadFolder()
    {
        var path = _settings.FolderPath;
        var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(path));
        FolderNameText.Text = string.IsNullOrEmpty(name) ? path : name; // drive roots have no file name
        TitleBar.ToolTip = path;
        Title = FolderNameText.Text;

        try
        {
            EntriesList.ItemsSource = FolderContents.List(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            EntriesList.ItemsSource = null;
            FolderNameText.Text += " (unavailable)";
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        // DragMove blocks until the mouse button is released, so this saves once, when dragging is complete.
        DragMove();
        SavePlacement();
    }

    private void ChangeFolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = App.PromptForFolder();
        if (folder is null)
        {
            return;
        }

        _settings.FolderPath = folder;
        LoadFolder();
        _saveSettings();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void EntriesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (EntriesList.SelectedItem is not FolderEntry entry)
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(entry.FullPath) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is Win32Exception or FileNotFoundException)
        {
            MessageBox.Show(this, $"Couldn't open \"{entry.Name}\".\n\n{ex.Message}", Title,
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Also captures size changes made with the resize grip.
        SavePlacement();
        base.OnClosing(e);
    }

    private void SavePlacement()
    {
        _settings.Left = Left;
        _settings.Top = Top;
        _settings.Width = ActualWidth;
        _settings.Height = ActualHeight;
        _saveSettings();
    }
}
