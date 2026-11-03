using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using FloatingWindowOrganizer.App.Interop;
using FloatingWindowOrganizer.Core.Models;
using FloatingWindowOrganizer.Core.Services;

namespace FloatingWindowOrganizer.App;

/// <summary>An iOS-style folder widget pinned to the desktop.</summary>
public partial class FolderWindow : Window
{
    public static readonly DependencyProperty GridColumnsProperty = DependencyProperty.Register(
        nameof(GridColumns), typeof(int), typeof(FolderWindow), new PropertyMetadata(4));

    public static readonly DependencyProperty GridRowsProperty = DependencyProperty.Register(
        nameof(GridRows), typeof(int), typeof(FolderWindow), new PropertyMetadata(2));

    private readonly App _host;

    /// <summary>Where the left button went down, while we wait to see if it becomes a drag.</summary>
    private Point? _pressPoint;

    public FolderWindow(FolderWindowSettings settings, App host)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(host);

        InitializeComponent();

        Settings = settings;
        _host = host;

        ApplySize();
        KeepOnScreen();
        LoadFolder();
    }

    public FolderWindowSettings Settings { get; }

    public int GridColumns
    {
        get => (int)GetValue(GridColumnsProperty);
        set => SetValue(GridColumnsProperty, value);
    }

    public int GridRows
    {
        get => (int)GetValue(GridRowsProperty);
        set => SetValue(GridRowsProperty, value);
    }

    private IntPtr Handle => new WindowInteropHelper(this).Handle;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        DesktopAttachment.Attach(Handle);
        WindowEffects.EnableBlurBehind(Handle);
        UpdateRoundedRegion();
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        // Show() puts a new window on top of everything; push it back down to the desktop layer.
        DesktopAttachment.SendToBottom(Handle);
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        UpdateRoundedRegion();
    }

    private void ApplySize()
    {
        var dimensions = WidgetLayout.For(Settings.Size);
        Width = dimensions.Width;
        Height = dimensions.Height;
        GridColumns = dimensions.Columns;
        GridRows = dimensions.Rows;
        UpdateRoundedRegion();
    }

    private void KeepOnScreen()
    {
        var desktop = new ScreenRect(
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth,
            SystemParameters.VirtualScreenHeight);
        (Left, Top) = WindowPlacement.EnsureVisible(Settings.Left, Settings.Top, Width, Height, desktop);
    }

    private void UpdateRoundedRegion()
    {
        var hwnd = Handle;
        if (hwnd == IntPtr.Zero)
        {
            return; // Not created yet; OnSourceInitialized will apply it.
        }

        var dpi = VisualTreeHelper.GetDpi(this);
        WindowEffects.SetRoundedRegion(
            hwnd,
            (int)Math.Ceiling(Width * dpi.DpiScaleX),
            (int)Math.Ceiling(Height * dpi.DpiScaleY),
            (int)Math.Round(WidgetLayout.CornerRadius * 2 * dpi.DpiScaleX));
    }

    private void LoadFolder()
    {
        var path = Settings.FolderPath;
        var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(path));
        FolderNameText.Text = string.IsNullOrEmpty(name) ? path : name; // drive roots have no file name
        Title = FolderNameText.Text;

        IReadOnlyList<FolderEntry> entries;
        try
        {
            entries = FolderContents.List(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            Tiles.ItemsSource = null;
            ItemCountText.Text = "Unavailable";
            return;
        }

        ItemCountText.Text = entries.Count.ToString(CultureInfo.CurrentCulture);

        var contents = WidgetLayout.Fit(entries, WidgetLayout.For(Settings.Size).Capacity);
        var tiles = contents.Visible
            .Select(entry => new TileViewModel
            {
                Label = entry.Name,
                TargetPath = entry.FullPath,
                Icon = ShellIcons.GetIcon(entry.FullPath),
            })
            .ToList();

        if (contents.HasOverflow)
        {
            tiles.Add(new TileViewModel
            {
                Label = "More",
                TargetPath = path,
                OverflowText = "+" + contents.OverflowCount.ToString(CultureInfo.CurrentCulture),
            });
        }

        Tiles.ItemsSource = tiles;
    }

    // ---- Mouse: double-click opens, press-and-move drags the widget ----

    private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;

        if (e.ClickCount == 2)
        {
            _pressPoint = null;
            if (TileFrom(e.OriginalSource) is { } tile)
            {
                Open(tile.TargetPath, tile.Label);
            }

            return;
        }

        _pressPoint = e.GetPosition(this);
        Card.CaptureMouse();
    }

    private void Card_MouseMove(object sender, MouseEventArgs e)
    {
        if (_pressPoint is not { } start || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var current = e.GetPosition(this);
        if (!DragThreshold.IsExceeded(
                current.X - start.X,
                current.Y - start.Y,
                SystemParameters.MinimumHorizontalDragDistance,
                SystemParameters.MinimumVerticalDragDistance))
        {
            return;
        }

        _pressPoint = null;
        Card.ReleaseMouseCapture();

        // DragMove blocks until the button is released, so this saves once, when dragging is complete.
        DragMove();
        DesktopAttachment.SendToBottom(Handle);
        SavePosition();
    }

    private void Card_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _pressPoint = null;
        Card.ReleaseMouseCapture();
    }

    private static TileViewModel? TileFrom(object source) => source switch
    {
        FrameworkElement element => element.DataContext as TileViewModel,
        FrameworkContentElement element => element.DataContext as TileViewModel,
        _ => null,
    };

    private void SavePosition()
    {
        Settings.Left = Left;
        Settings.Top = Top;
        _host.SaveSettings();
    }

    private void Open(string path, string displayName)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is Win32Exception or FileNotFoundException)
        {
            MessageBox.Show(
                $"Couldn't open \"{displayName}\".\n\n{ex.Message}",
                Title,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    // ---- Right-click menu ----

    private void ContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        SizeSmallItem.IsChecked = Settings.Size == WidgetSize.Small;
        SizeMediumItem.IsChecked = Settings.Size == WidgetSize.Medium;
        SizeLargeItem.IsChecked = Settings.Size == WidgetSize.Large;
    }

    private void OpenInExplorer_Click(object sender, RoutedEventArgs e) =>
        Open(Settings.FolderPath, FolderNameText.Text);

    private void ChangeFolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = App.PromptForFolder();
        if (folder is null)
        {
            return;
        }

        Settings.FolderPath = folder;
        LoadFolder();
        _host.SaveSettings();
    }

    private void Size_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string tag } || !Enum.TryParse<WidgetSize>(tag, out var size))
        {
            return;
        }

        Settings.Size = size;
        ApplySize();
        KeepOnScreen(); // a bigger size may push the widget past the screen edge
        Settings.Left = Left;
        Settings.Top = Top;
        LoadFolder();
        _host.SaveSettings();
    }

    private void AddWidget_Click(object sender, RoutedEventArgs e) => _host.AddWidget();

    private void RemoveWidget_Click(object sender, RoutedEventArgs e)
    {
        var answer = MessageBox.Show(
            $"Remove the \"{FolderNameText.Text}\" widget? The folder itself isn't touched.",
            "Remove widget",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer == MessageBoxResult.Yes)
        {
            _host.RemoveWidget(this);
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
}
