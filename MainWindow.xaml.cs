using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text.Json;
using System.IO;
using System.Windows.Navigation;
using UtilityMaster.Services;

namespace UtilityMaster;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        var settings = SettingsService.Load();
        Loc.SetLanguage(settings.Language);
        InitializeComponent();
        ContentFrame.NavigationUIVisibility = NavigationUIVisibility.Hidden;
        Title = Loc.Get("window.title");
        Loaded += OnLoaded;
        Closing += OnClosing;
        KeyDown += OnKeyDown;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var wp = LoadWindowPosition();
        if (wp.Width > 0) { Width = wp.Width; Height = wp.Height; Left = wp.Left; Top = wp.Top; }
        ContentFrame.Content = new Views.HomePage("nades");
        // Highlight N nav
        _activeNav = "nades";
        UpdateNavHighlights();
    }

    private string _activeNav = "nades";

    private void UpdateNavHighlights()
    {
        // N button
        if (FindName("NNav") is Border nNav)
        {
            nNav.Background = _activeNav == "nades"
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xf5, 0xa6, 0x23))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1a, 0x1d, 0x24));
            ((TextBlock)nNav.Child).Foreground = _activeNav == "nades"
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x0f, 0x11, 0x16))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x88, 0x88, 0x88));
        }
        // T button
        if (FindName("TNav") is Border tNav)
        {
            tNav.Background = _activeNav == "tricks"
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xf5, 0xa6, 0x23))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1a, 0x1d, 0x24));
            ((TextBlock)tNav.Child).Foreground = _activeNav == "tricks"
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x0f, 0x11, 0x16))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x88, 0x88, 0x88));
        }
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e) => SaveWindowPosition();

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N)
        {
            if (ContentFrame.Content is Views.MapView mv) mv.OpenCreateTargetAt(0, 0);
        }
    }

    private void NadesNav_Click(object sender, MouseButtonEventArgs e)
    {
        _activeNav = "nades";
        UpdateNavHighlights();
        if (ContentFrame.Content is Views.MapView mv && mv.MapId != null)
            ContentFrame.Content = new Views.MapView(mv.MapId, "nades");
        else
            ContentFrame.Content = new Views.HomePage("nades");
    }

    private void TricksNav_Click(object sender, MouseButtonEventArgs e)
    {
        _activeNav = "tricks";
        UpdateNavHighlights();
        if (ContentFrame.Content is Views.MapView mv && mv.MapId != null)
            ContentFrame.Content = new Views.MapView(mv.MapId, "tricks");
        else
            ContentFrame.Content = new Views.HomePage("tricks");
    }

    private void SettingsNav_Click(object sender, MouseButtonEventArgs e) => ContentFrame.Content = new Views.SettingsPage();
    private void AboutNav_Click(object sender, MouseButtonEventArgs e) => ContentFrame.Content = new Views.AboutPage();

    private static string PosFile => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "UtilityMaster", "window.json");

    private (double Width, double Height, double Left, double Top) LoadWindowPosition()
    {
        try { if (File.Exists(PosFile)) return JsonSerializer.Deserialize<(double, double, double, double)>(File.ReadAllText(PosFile)); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
        return (0, 0, 0, 0);
    }

    private void SaveWindowPosition()
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(PosFile)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            double l = double.IsNaN(Left) ? 0 : Left; double t = double.IsNaN(Top) ? 0 : Top; File.WriteAllText(PosFile, JsonSerializer.Serialize((Width, Height, l, t)));
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
    }
}
