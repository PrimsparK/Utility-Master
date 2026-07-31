using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using UtilityMaster.Models;
using UtilityMaster.Services;

namespace UtilityMaster.Views;

public partial class HomePage : Page
{
    public static readonly List<MapInfo> Maps = new()
    {
        new() { Id = "de_dust2", DisplayName = "Dust2", RadarPath = "Maps/de_dust2_radar_psd.png", HasLowerFloor = false, PosX = -2530, PosY = 3001, Scale = 4.66f },
        new() { Id = "de_mirage", DisplayName = "Mirage", RadarPath = "Maps/de_mirage_radar_psd.png", HasLowerFloor = false, PosX = -3231, PosY = 1861, Scale = 5f },
        new() { Id = "de_inferno", DisplayName = "Inferno", RadarPath = "Maps/de_inferno_radar_psd.png", HasLowerFloor = false, PosX = -2087, PosY = 3840, Scale = 4.9f },
        new() { Id = "de_nuke", DisplayName = "Nuke", RadarPath = "Maps/de_nuke_radar_psd.png", LowerRadarPath = "Maps/de_nuke_lower_radar_psd.png", HasLowerFloor = true, PosX = -3453, PosY = 2887, Scale = 6f },
        new() { Id = "de_ancient", DisplayName = "Ancient", RadarPath = "Maps/de_ancient_radar_psd.png", HasLowerFloor = false, PosX = -2953, PosY = 2164, Scale = 5f },
        new() { Id = "de_anubis", DisplayName = "Anubis", RadarPath = "Maps/de_anubis_radar_psd.png", HasLowerFloor = false, PosX = -2796, PosY = 3328, Scale = 5.22f },
        new() { Id = "de_cache", DisplayName = "Cache", RadarPath = "Maps/de_cache_radar_psd.png", HasLowerFloor = false, PosX = -2000, PosY = 3250, Scale = 5.5f },
        new() { Id = "de_overpass", DisplayName = "Overpass", RadarPath = "Maps/de_overpass_radar_psd.png", HasLowerFloor = false, PosX = -4831, PosY = 1781, Scale = 5.2f },
        new() { Id = "de_train", DisplayName = "Train", RadarPath = "Maps/de_train_radar_psd.png", LowerRadarPath = "Maps/de_train_lower_radar_psd.png", HasLowerFloor = true, PosX = -2308, PosY = 2078, Scale = 4.08f },
        new() { Id = "de_vertigo", DisplayName = "Vertigo", RadarPath = "Maps/de_vertigo_radar_psd.png", LowerRadarPath = "Maps/de_vertigo_lower_radar_psd.png", HasLowerFloor = true, PosX = -3168, PosY = 1762, Scale = 4f },
    };

    private string _mode = "nades";

    public HomePage(string mode = "nades")
    {
        _mode = mode;
        InitializeComponent();
        Loaded += (_, _) => { BuildMapCards(); UpdateModePills(); UpdatePillTexts(); };
    }

    private void UpdateModePills()
    {
        var actBg = new SolidColorBrush(Color.FromRgb(0xf5, 0xa6, 0x23));
        var defBg = new SolidColorBrush(Color.FromRgb(0x1a, 0x1d, 0x24));
        var actFg = new SolidColorBrush(Color.FromRgb(0x0f, 0x11, 0x16));
        var defFg = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));

        if (NadesPill != null)
        {
            NadesPill.Background = _mode == "nades" ? actBg : defBg;
            NadesPillText.Foreground = _mode == "nades" ? actFg : defFg;
        }
        if (TricksPill != null)
        {
            TricksPill.Background = _mode == "tricks" ? actBg : defBg;
            TricksPillText.Foreground = _mode == "tricks" ? actFg : defFg;
        }
    }

    private void UpdatePillTexts()
    {
        NadesPillText.Text = Loc.Get("nades");
        TricksPillText.Text = Loc.Get("tricks");
        SettingsPillText.Text = Loc.Get("settings.nav");
        AboutPillText.Text = Loc.Get("about");
    }

    private void BuildMapCards()
    {
        MapsPanel.Children.Clear();
        var assetsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Maps");
        bool showCn = SettingsService.Load().UseChineseTerms;

        foreach (var map in Maps)
        {
            var card = new Border
            {
            Width = 180, Height = 150, Margin = new Thickness(8),
            CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(Color.FromRgb(0x14, 0x16, 0x1c)),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 12,
                ShadowDepth = 2,
                Color = Color.FromRgb(0, 0, 0),
                Opacity = 0.3
            },
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x2a, 0x2d, 0x34)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var iconFile = Path.Combine(assetsPath, map.Id + ".png");
            if (File.Exists(iconFile))
            {
                var img = new Image
                {
                    Source = new BitmapImage(new Uri(iconFile)),
                    Stretch = Stretch.Uniform,
                    Margin = new Thickness(12, 12, 12, 4),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(img, 0);
                grid.Children.Add(img);
            }

            var text = new TextBlock
            {
                Text = showCn ? MapNames.GetChineseName(map.Id) : map.DisplayName,
                FontSize = 13, FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xcc, 0xcc, 0xcc)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(text, 1);
            grid.Children.Add(text);

            card.Child = grid;
            card.MouseEnter += (_, _) =>
            {
                card.BorderBrush = new SolidColorBrush(Color.FromRgb(0xf5, 0xa6, 0x23));
                card.Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x1d, 0x26));
            };
            card.MouseLeave += (_, _) =>
            {
                card.BorderBrush = new SolidColorBrush(Color.FromRgb(0x2a, 0x2d, 0x34));
                card.Background = new SolidColorBrush(Color.FromRgb(0x14, 0x16, 0x1c));
            };

            var mapId = map.Id;
            var mode = _mode;
            card.MouseLeftButtonDown += (_, _) =>
            {
                if (Window.GetWindow(this) is MainWindow main)
                    main.ContentFrame.Content = new MapView(mapId, mode);
            };

            MapsPanel.Children.Add(card);
        }
    }

    private void NadesNav_Click(object sender, MouseButtonEventArgs e)
    {
        var tag = ((FrameworkElement)sender).Tag?.ToString();
        if (tag == "nades") { _mode = "nades"; BuildMapCards(); UpdateModePills(); }
        else if (tag == "tricks") { _mode = "tricks"; BuildMapCards(); UpdateModePills(); }
    }

    private void Nav_Click(object sender, MouseButtonEventArgs e)
    {
        var tag = ((FrameworkElement)sender).Tag?.ToString();
        if (Window.GetWindow(this) is MainWindow main)
        {
            if (tag == "settings") main.ContentFrame.Content = new SettingsPage();
            else if (tag == "about") main.ContentFrame.Content = new AboutPage();
        }
    }
}
