using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using UtilityMaster.Models;
using UtilityMaster.Services;

namespace UtilityMaster.Views;

public partial class MapView : Page
{
    private readonly string _mapId;
    public string MapId => _mapId;
    private readonly string _mode;
    public string Mode => _mode;
    private MapInfo? _mapInfo;
    private string _activeType = "smoke";
    private string _activeSide = "Both";
    private string _activeFloor = "default";
    private bool _proOnly;
    private bool _isPanning;
    private Point _panStart;
    private Point _translateBeforePan;
    private double _zoom = 1.0;
    private double _minZoom;
    private Ellipse? _previewMarker;
    private IDataService? _dataService;
    private ProfileEntity? _activeProfile;
    private List<TargetEntity> _mapTargets = new();
    private List<TrickEntity> _mapTricks = new();
    private readonly string _iconsPath;
    private Guid? _highlightedTargetId;
    private Guid? _highlightedTrickId;
    private bool _didDrag;
    private string _nadesBrowseMode = "targets";
    private readonly HashSet<string> _lineupTypes = new() { "smoke", "flash", "he", "molotov" };
    private (double X, double Y)? _selectedLineupSpot;
    private List<LineupSpot> _lineupSpots = new();
    private List<TargetEntity> _visibleLineupTargets = new();
    private readonly HashSet<Guid> _drawnTargetIds = new();

    public MapView(string mapId, string mode = "nades")
    {
        _mapId = mapId;
        _mode = mode;
        InitializeComponent();
        _iconsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Maps");
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _mapInfo = MapCatalog.Maps.FirstOrDefault(m => m.Id == _mapId);
        if (_mapInfo == null) return;

        // Show correct filter bar
        NadesFilterBar.Visibility = _mode == "nades" ? Visibility.Visible : Visibility.Collapsed;
        TricksFilterBar.Visibility = _mode == "tricks" ? Visibility.Visible : Visibility.Collapsed;

        PopulateMapSwitchers();
        var assetsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
        var radarPath = System.IO.Path.Combine(assetsPath, _mapInfo.RadarPath);
        if (File.Exists(radarPath))
        {
            var bmp = new BitmapImage(); bmp.BeginInit(); bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(radarPath); bmp.EndInit(); bmp.Freeze();
            MapImage.Source = bmp;
        }
        if (_mapInfo.HasLowerFloor) { FloorPanel.Visibility = Visibility.Visible; TricksFloorPanel.Visibility = Visibility.Visible; }
        _dataService = ((App)Application.Current).DataService;
        _activeProfile = _dataService.GetActiveProfile(); if (_activeProfile != null) { var sets = SettingsService.Load(); _activeProfile.AllowDeleteDefaultSpots = sets.AllowDeleteDefaults; }
        if (_mode == "nades") { ReloadTargets(); UpdateFilterHighlights(); } else { _activeType = "wallbang"; ReloadTricks(); UpdateTrickFilterHighlights(); }
        ApplyLocalization();
        Dispatcher.BeginInvoke(new Action(() => TryDelayedInit()), System.Windows.Threading.DispatcherPriority.Loaded);
    }

    private void PopulateMapSwitchers()
    {
        bool showCn = SettingsService.Load().UseChineseTerms;
        // Create MapInfo copies with localized display names
        var items = MapCatalog.Maps.Select(m => new MapInfo
        {
            Id = m.Id,
            DisplayName = showCn ? MapNames.GetChineseName(m.Id) : m.DisplayName,
            RadarPath = m.RadarPath,
            LowerRadarPath = m.LowerRadarPath,
            HasLowerFloor = m.HasLowerFloor,
            PosX = m.PosX,
            PosY = m.PosY,
            Scale = m.Scale
        }).ToList();

        foreach (var map in items)
        {
            var item1 = new ComboBoxItem { Content = map.DisplayName, Tag = map.Id, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xcc, 0xcc, 0xcc)), FontSize = 13 };
            if (map.Id == _mapId) item1.IsSelected = true;
            MapSwitcherNades.Items.Add(item1);
            
            var item2 = new ComboBoxItem { Content = map.DisplayName, Tag = map.Id, Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xcc, 0xcc, 0xcc)), FontSize = 13 };
            if (map.Id == _mapId) item2.IsSelected = true;
            MapSwitcherTricks.Items.Add(item2);
        }
    }

    private void MapSwitcher_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox cb && cb.SelectedItem is ComboBoxItem cbi && cbi.Tag is string mapId && mapId != _mapId)
        {
            if (Window.GetWindow(this) is MainWindow mw)
                mw.ContentFrame.Content = new MapView(mapId, _mode);
        }
    }


    private void ApplyLocalization()
    {
        FloorDefault.Text = Loc.Get("upper");
        FloorLower.Text = Loc.Get("lower");
        TricksFloorDefault.Text = Loc.Get("upper");
        TricksFloorLower.Text = Loc.Get("lower");
        TrickWallbangLabel.Text = Loc.Get("wallbang");
        TrickBoostLabel.Text = Loc.Get("boost");
        TrickJumpLabel.Text = Loc.Get("jump");
        TrickCampLabel.Text = Loc.Get("camp");
        ProPillText.Text = Loc.Get("pro");
        TrickProPillText.Text = Loc.Get("pro");
        TargetBrowseText.Text = Loc.Get("map.browse_targets");
        LineupBrowseText.Text = Loc.Get("map.browse_lineups");
        SearchTitleLabel.Text = Loc.Get("map.search_title");
        SearchBox.ToolTip = Loc.Get("map.search_placeholder");
        SearchResultCount.Text = "";
        SearchEmptyText.Text = Loc.Get("map.search_no_results");
        SearchHintText.Text = Loc.Get("map.search_hint");
        SideToggleBtn.ToolTip = Loc.Get("map.search_show");
        SideCollapseBtn.ToolTip = Loc.Get("map.search_collapse");
        ClearSearchBtn.ToolTip = Loc.Get("map.search_clear");
    }

    private Border MapBorder => (Border)MapCanvas.Parent;
    private double ContainerW => MapBorder.ActualWidth;
    private double ContainerH => MapBorder.ActualHeight;
    private double ImageW => MapImage.Source?.Width ?? 1024;
    private double ImageH => MapImage.Source?.Height ?? 1024;

    private void TryDelayedInit()
    {
        if (ContainerW > 0 && ContainerH > 0 && MapImage.Source != null) { if (_minZoom < 0.001) { _minZoom = Math.Max(0.1, Math.Min(ContainerW / ImageW, ContainerH / ImageH)); _zoom = _minZoom; ApplyZoom(); CenterMap(); } return; }
        Dispatcher.BeginInvoke(new Action(() => TryDelayedInit()), System.Windows.Threading.DispatcherPriority.Background);
    }

    private void ApplyZoom() { MapScale.ScaleX = _zoom; MapScale.ScaleY = _zoom; }
    private void CenterMap() { MapTranslate.X = (ContainerW - ImageW * _zoom) / 2; MapTranslate.Y = (ContainerH - ImageH * _zoom) / 2; }

    private void ClampMap()
    {
        if (ContainerW <= 0 || ContainerH <= 0 || MapImage.Source == null) return;
        var sw = ImageW * _zoom; var sh = ImageH * _zoom;
        if (sw <= ContainerW) MapTranslate.X = (ContainerW - sw) / 2; else MapTranslate.X = Math.Max(ContainerW - sw, Math.Min(0, MapTranslate.X));
        if (sh <= ContainerH) MapTranslate.Y = (ContainerH - sh) / 2; else MapTranslate.Y = Math.Max(ContainerH - sh, Math.Min(0, MapTranslate.Y));
    }

    private void Container_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (ContainerW <= 0 || ContainerH <= 0 || MapImage.Source == null) return;
        var nm = Math.Max(0.1, Math.Min(ContainerW / ImageW, ContainerH / ImageH));
        if (_minZoom < 0.001) { _minZoom = nm; _zoom = _minZoom; ApplyZoom(); CenterMap(); }
        else { _minZoom = nm; if (_zoom < _minZoom) { _zoom = _minZoom; ApplyZoom(); } ClampMap(); }
    }

    private void ReloadTargets()
    {
        if (_dataService == null || _activeProfile == null) return;
        _mapTargets = _dataService.GetAllTargets(_activeProfile.Id, _mapId);
        if (_mode == "tricks") ReloadTricks();
        else DrawNades();
    }

    private void DrawNades()
    {
        if (_nadesBrowseMode == "lineups") DrawLineups();
        else DrawTargets();
    }

    private void ReloadTricks()
    {
        if (_dataService == null || _activeProfile == null) return;
        _mapTricks = _dataService.GetTricks(_activeProfile.Id, _mapId);
        _mapTargets = _dataService.GetAllTargets(_activeProfile.Id, _mapId).Where(t => t.Type == "wallbang" || t.Type == "jump").ToList();
        DrawTricks();
    }

    private void ShowPreviewMarker(double x, double y)
    {
        RemovePreviewMarker();
        _previewMarker = new Ellipse { Width = 24, Height = 24, Fill = new SolidColorBrush(Color.FromRgb(0xf5, 0xa6, 0x23)) { Opacity = 0.5 }, Stroke = Brushes.White, StrokeThickness = 1.5, Tag = "preview" };
        Canvas.SetLeft(_previewMarker, x - 12); Canvas.SetTop(_previewMarker, y - 12); MapCanvas.Children.Add(_previewMarker);
    }
    private void RemovePreviewMarker()
    {
        if (_previewMarker != null) MapCanvas.Children.Remove(_previewMarker);
        foreach (var o in MapCanvas.Children.OfType<FrameworkElement>().Where(c => (string?)c.Tag == "preview").ToList()) MapCanvas.Children.Remove(o);
        _previewMarker = null;
    }

    private void ClearMapOverlays()
    {
        foreach (var c in MapCanvas.Children.OfType<FrameworkElement>().Where(c => { var t = c.Tag?.ToString() ?? ""; return t.StartsWith("t:") || t.StartsWith("l:") || t.StartsWith("tr:") || t.StartsWith("ls") || t == "dash" || t == "glow" || t == "lineup-preview"; }).ToList()) MapCanvas.Children.Remove(c);
        RemovePreviewMarker();
    }

    private Point ScreenToCanvas(Point s) => new((s.X - MapTranslate.X) / _zoom, (s.Y - MapTranslate.Y) / _zoom);
    private static double Dist(Point p, double x, double y) => Math.Sqrt((p.X - x) * (p.X - x) + (p.Y - y) * (p.Y - y));

    private void ShowLineupWindowWithPick(AddLineupWindow lw, Action<AddLineupWindow> onComplete)
    {
        lw.Owner = Window.GetWindow(this);
        lw.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
        lw.CloseCallback = lw2 => {
            lw2.Close();
            onComplete(lw2);
        };
        lw.Show();
    }


    public void EnterPositionPickMode(double startX, double startY, Action<double, double> onPicked)
    {
        _positionPickCallback = onPicked;
        _positionPickActive = true;
        _positionPickX = startX;
        _positionPickY = startY;
        // Show lineup icon preview at start position on the map canvas
        _positionPickMarker = CreateLineupPreviewIcon();
        Canvas.SetLeft(_positionPickMarker, startX - 13);
        Canvas.SetTop(_positionPickMarker, startY - 13);
        MapCanvas.Children.Add(_positionPickMarker);
        MapBorder.Cursor = System.Windows.Input.Cursors.Cross;
    }

    private void CompletePositionPick(double canvasX, double canvasY)
    {
        if (!_positionPickActive) return;
        var cb = _positionPickCallback;
        _positionPickActive = false;
        _positionPickCallback = null;
        MapBorder.Cursor = null;
        if (_positionPickMarker != null) { MapCanvas.Children.Remove(_positionPickMarker); _positionPickMarker = null; }
        cb?.Invoke(canvasX, canvasY);
    }

    private Border CreateLineupPreviewIcon()
    {
        var g = new System.Windows.Controls.Grid();
        g.Children.Add(new System.Windows.Shapes.Ellipse { Width = 20, Height = 20, Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xf5, 0xa6, 0x23)), Stroke = System.Windows.Media.Brushes.White, StrokeThickness = 2 });
        g.Children.Add(new System.Windows.Controls.TextBlock { Text = "?", FontSize = 10, FontWeight = System.Windows.FontWeights.Bold, Foreground = System.Windows.Media.Brushes.Black, HorizontalAlignment = System.Windows.HorizontalAlignment.Center, VerticalAlignment = System.Windows.VerticalAlignment.Center });
        return new System.Windows.Controls.Border { Child = g, Tag = "pick-preview", Background = System.Windows.Media.Brushes.Transparent, Width = 26, Height = 26, IsHitTestVisible = false };
    }

    private Border? _positionPickMarker;

    private bool _positionPickActive;
    private double _positionPickX, _positionPickY;
    private Action<double, double>? _positionPickCallback;

}
