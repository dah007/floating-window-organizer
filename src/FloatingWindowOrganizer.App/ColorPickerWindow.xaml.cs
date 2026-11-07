using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FloatingWindowOrganizer.Core.Models;
using FloatingWindowOrganizer.Core.Services;

namespace FloatingWindowOrganizer.App;

/// <summary>
/// Picks a widget background: saturation/brightness square, rainbow hue bar, opacity bar, hex box
/// and presets. <see cref="ColorChanged"/> fires on every change so the widget can preview it live.
/// </summary>
public partial class ColorPickerWindow : Window
{
    /// <summary>Opacity a first-time colour starts at: the same 72% as the theme glass.</summary>
    public const byte DefaultAlpha = 0xB8;

    private static readonly (string Name, uint Rgb)[] Presets =
    [
        ("Red", 0xFF3B30), ("Orange", 0xFF9500), ("Yellow", 0xFFCC00), ("Green", 0x34C759),
        ("Teal", 0x30B0C7), ("Blue", 0x007AFF), ("Indigo", 0x5856D6), ("Purple", 0xAF52DE),
        ("Pink", 0xFF2D55), ("Brown", 0xA2845E), ("Graphite", 0x1C1C1E), ("White", 0xF2F2F7),
    ];

    private double _hue;
    private double _saturation;
    private double _value;
    private double _alpha;

    public ColorPickerWindow(uint? currentColor)
    {
        InitializeComponent();

        var start = currentColor ?? ColorMath.ToArgb(DefaultAlpha, 0x00, 0x7A, 0xFF);
        SetColor(start, keepAlpha: false, raiseChanged: false);
        SelectedColor = currentColor;

        foreach (var (name, rgb) in Presets)
        {
            var swatch = new Button
            {
                Style = (Style)FindResource("SwatchButton"),
                Background = WidgetTheme.ToBrush(0xFF000000 | rgb),
                ToolTip = name,
            };
            AutomationProperties.SetName(swatch, name);
            swatch.Click += (_, _) => SetColor(0xFF000000 | rgb, keepAlpha: true, raiseChanged: true);
            PresetPanel.Children.Add(swatch);
        }

        Loaded += (_, _) => UpdateVisuals();
    }

    /// <summary>Raised whenever the colour changes, for a live preview.</summary>
    public event EventHandler? ColorChanged;

    /// <summary>
    /// The chosen colour (#AARRGGBB), or null when the user picked Default (follow the Windows theme).
    /// Only meaningful after the dialog returns true.
    /// </summary>
    public uint? SelectedColor { get; private set; }

    private uint CurrentArgb
    {
        get
        {
            var (r, g, b) = ColorMath.HsvToRgb(_hue, _saturation, _value);
            return ColorMath.ToArgb((byte)Math.Round(_alpha * 255), r, g, b);
        }
    }

    private void SetColor(uint argb, bool keepAlpha, bool raiseChanged)
    {
        var (a, r, g, b) = ColorMath.Split(argb);
        var (hue, saturation, value) = ColorMath.RgbToHsv(r, g, b);

        // Greys have no hue; keep the bar where it was rather than snapping to red.
        if (saturation > 0)
        {
            _hue = hue;
        }

        _saturation = saturation;
        _value = value;
        if (!keepAlpha)
        {
            _alpha = a / 255.0;
        }

        UpdateVisuals();
        if (raiseChanged)
        {
            OnColorChanged();
        }
    }

    private void OnColorChanged()
    {
        SelectedColor = CurrentArgb;
        ColorChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateVisuals()
    {
        var argb = CurrentArgb;
        var (hr, hg, hb) = ColorMath.HsvToRgb(_hue, 1, 1);
        var (_, r, g, b) = ColorMath.Split(argb);

        HueFill.Background = WidgetTheme.ToBrush(ColorMath.ToArgb(0xFF, hr, hg, hb));
        PreviewSwatch.Background = WidgetTheme.ToBrush(argb);
        PreviewText.Foreground = WidgetTheme.ToBrush(WidgetPalette.ForBackground(argb).Text);

        var alphaFill = new LinearGradientBrush(
            Color.FromArgb(0, r, g, b), Color.FromArgb(0xFF, r, g, b), new Point(0, 0), new Point(1, 0));
        alphaFill.Freeze();
        AlphaFill.Background = alphaFill;

        Place(SvThumb, _saturation * SvArea.ActualWidth, (1 - _value) * SvArea.ActualHeight);
        Place(HueThumb, _hue / 360 * HueArea.ActualWidth, null);
        Place(AlphaThumb, _alpha * AlphaArea.ActualWidth, null);

        OpacityText.Text = $"{Math.Round(_alpha * 100)}%";
        if (!HexBox.IsKeyboardFocused)
        {
            HexBox.Text = ColorMath.ToRgbHex(argb);
        }
    }

    private static void Place(FrameworkElement thumb, double centerX, double? centerY)
    {
        Canvas.SetLeft(thumb, centerX - (thumb.Width / 2));
        if (centerY is { } y)
        {
            Canvas.SetTop(thumb, y - (thumb.Height / 2));
        }
    }

    // ---- Dragging on the square and the two bars ----

    private void Area_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var area = (FrameworkElement)sender;
        area.CaptureMouse();
        PickAt(area, e.GetPosition(area));
        e.Handled = true;
    }

    private void Area_MouseMove(object sender, MouseEventArgs e)
    {
        var area = (FrameworkElement)sender;
        if (area.IsMouseCaptured)
        {
            PickAt(area, e.GetPosition(area));
        }
    }

    private void Area_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) =>
        ((FrameworkElement)sender).ReleaseMouseCapture();

    private void Area_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateVisuals();

    private void PickAt(FrameworkElement area, Point point)
    {
        var x = area.ActualWidth > 0 ? Math.Clamp(point.X / area.ActualWidth, 0, 1) : 0;
        var y = area.ActualHeight > 0 ? Math.Clamp(point.Y / area.ActualHeight, 0, 1) : 0;

        if (area == SvArea)
        {
            _saturation = x;
            _value = 1 - y;
        }
        else if (area == HueArea)
        {
            _hue = x * 360;
        }
        else
        {
            _alpha = x;
        }

        UpdateVisuals();
        OnColorChanged();
    }

    // ---- Hex entry ----

    private void HexBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ApplyHex();
            e.Handled = true; // Enter applies the hex; it doesn't also close the dialog
        }
    }

    private void HexBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => ApplyHex();

    private void ApplyHex()
    {
        if (ColorMath.TryParseHex(HexBox.Text, out var argb, out var hasAlpha))
        {
            SetColor(argb, keepAlpha: !hasAlpha, raiseChanged: true);
        }

        HexBox.Text = ColorMath.ToRgbHex(CurrentArgb); // normalise, or undo an unreadable entry
    }

    // ---- Buttons ----

    private void Default_Click(object sender, RoutedEventArgs e)
    {
        SelectedColor = null;
        DialogResult = true;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (HexBox.IsKeyboardFocused)
        {
            ApplyHex();
        }

        SelectedColor = CurrentArgb;
        DialogResult = true;
    }
}
