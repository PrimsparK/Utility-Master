using System.Windows.Controls;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using System.Linq;
using UtilityMaster.Models;
using UtilityMaster.Services;

namespace UtilityMaster.Views;
public partial class MapView : Page
{
    private readonly string _mapId;
    public string MapId => _mapId;
    private readonly string _mode;
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
        _mapInfo = HomePage.Maps.FirstOrDefault(m => m.Id == _mapId);
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
        var items = HomePage.Maps.Select(m => new MapInfo
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
        DrawTargets();
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
        foreach (var c in MapCanvas.Children.OfType<FrameworkElement>().Where(c => { var t = c.Tag?.ToString() ?? ""; return t.StartsWith("t:") || t.StartsWith("l:") || t.StartsWith("tr:") || t == "dash" || t == "glow" || t == "lineup-preview"; }).ToList()) MapCanvas.Children.Remove(c);
        RemovePreviewMarker();
    }

    // ===== NADES =====
    private void DrawTargets()
    {
        var visible = _mapTargets.Where(t => t.Type == _activeType && (_activeSide == "Both" || t.Side == _activeSide) && t.Floor == _activeFloor && (!_proOnly || t.Lineups.Any(l => l.IsPro))).ToList();
        
        // Remove old nades overlays
        // Clear border highlights from target icons
        foreach (var b in MapCanvas.Children.OfType<Border>().Where(b => { var t = b.Tag?.ToString() ?? ""; return t.StartsWith("t:"); }))
            b.BorderBrush = Brushes.Transparent;
        foreach (var c in MapCanvas.Children.OfType<FrameworkElement>().Where(c => { var t = c.Tag?.ToString() ?? ""; return t.StartsWith("t:") || t.StartsWith("l:") || t == "dash" || t == "glow"; }).ToList())
            MapCanvas.Children.Remove(c);
        RemovePreviewMarker();
        
        foreach (var t in visible) { var i = CreateTargetIcon(t); Canvas.SetLeft(i, t.X - 16); Canvas.SetTop(i, t.Y - 16); MapCanvas.Children.Add(i); }
        if (_highlightedTargetId.HasValue && visible.Any(t => t.Id == _highlightedTargetId.Value)) DrawHighlight(_highlightedTargetId.Value);
        else _highlightedTargetId = null;
    }

    private Border CreateTargetIcon(TargetEntity target)
    {
        var g = new Grid();
        g.Children.Add(new Ellipse { Width = 28, Height = 28, Fill = new SolidColorBrush(Color.FromRgb(0x1a, 0x1d, 0x24)), Stroke = new SolidColorBrush(Color.FromRgb(0x3a, 0x3d, 0x44)), StrokeThickness = 2 });
        var img = new Image { Width = 18, Height = 18, Stretch = Stretch.Uniform };
        var fp = System.IO.Path.Combine(_iconsPath, GetTargetIconFile(target.Type, target.Side));
        if (File.Exists(fp)) { var bmp = new BitmapImage(); bmp.BeginInit(); bmp.CacheOption = BitmapCacheOption.OnLoad; bmp.UriSource = new Uri(fp); bmp.EndInit(); bmp.Freeze(); img.Source = bmp; }
        g.Children.Add(img);
        var lineupCount = _proOnly ? target.Lineups.Count(l => l.IsPro) : target.Lineups.Count;
        if (lineupCount >= 2) g.Children.Add(new Border { Background = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa)), BorderThickness = new Thickness(0.6), CornerRadius = new CornerRadius(4), Width = 10, Height = 10, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 1, 1), Child = new TextBlock { Text = lineupCount.ToString(), FontSize = 6, FontWeight = FontWeights.Bold, Foreground = Brushes.Black, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } });
        var b = new Border { Child = g, Tag = "t:" + target.Id, Background = Brushes.Transparent, Width = 32, Height = 32, IsHitTestVisible = true };
        Panel.SetZIndex(b, 10);
        return b;
    }

        private Border CreateLineupIcon(LineupEntity lineup, int variantCount = 1)
    {
        var side = lineup.Side ?? "T";
        Brush fillBrush;
        if (side == "Both")
        {
            var dg = new DrawingGroup();
            var geo1 = new StreamGeometry();
            using (var ctx = geo1.Open()) { ctx.BeginFigure(new Point(0,0), true, true); ctx.LineTo(new Point(22,0), true, false); ctx.LineTo(new Point(0,22), true, false); }
            dg.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(0xde,0x7b,0x2c)), null, geo1));
            var geo2 = new StreamGeometry();
            using (var ctx = geo2.Open()) { ctx.BeginFigure(new Point(22,0), true, true); ctx.LineTo(new Point(22,22), true, false); ctx.LineTo(new Point(0,22), true, false); }
            dg.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(0x3b,0x7c,0xc7)), null, geo2));
            fillBrush = new DrawingBrush(dg) { Stretch = Stretch.None, AlignmentX = AlignmentX.Center, AlignmentY = AlignmentY.Center };
        }
        else if (side == "CT")
            fillBrush = new SolidColorBrush(Color.FromRgb(0x3b, 0x7c, 0xc7));
        else
            fillBrush = new SolidColorBrush(Color.FromRgb(0xde, 0x7b, 0x2c));

        var g = new Grid();
        g.Children.Add(new Ellipse { Width = 20, Height = 20, Fill = fillBrush, Stroke = new SolidColorBrush(Color.FromRgb(0x1a, 0x1d, 0x24)), StrokeThickness = 1.5 });
        g.Children.Add(new TextBlock { Text = lineup.Sequence.ToString(), FontSize = 10, FontWeight = FontWeights.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
        return new Border { Child = g, Tag = "l:" + lineup.Id, Background = Brushes.Transparent, Width = 24, Height = 24, IsHitTestVisible = true };
    }

    private Border CreateTrickTargetIcon(TargetEntity target)
    {
        var g = new Grid();
        var color = target.Type switch { "wallbang" => Color.FromRgb(0xe7, 0x4c, 0x3c), "jump" => Color.FromRgb(0x2e, 0xcc, 0x71), _ => Color.FromRgb(0x88, 0x88, 0x88) };
        var label = target.Type switch { "wallbang" => "W", "jump" => "J", _ => "?" };
        g.Children.Add(new Ellipse { Width = 28, Height = 28, Fill = new SolidColorBrush(color), Stroke = new SolidColorBrush(Color.FromRgb(0x1a, 0x1d, 0x24)), StrokeThickness = 2 });
        g.Children.Add(new TextBlock { Text = label, FontSize = 12, FontWeight = FontWeights.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
        var lineupCount = _proOnly ? target.Lineups.Count(l => l.IsPro) : target.Lineups.Count;
        if (lineupCount >= 2) g.Children.Add(new Border { Background = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa)), BorderThickness = new Thickness(0.6), CornerRadius = new CornerRadius(4), Width = 10, Height = 10, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 1, 1), Child = new TextBlock { Text = lineupCount.ToString(), FontSize = 6, FontWeight = FontWeights.Bold, Foreground = Brushes.Black, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } });
        var b = new Border { Child = g, Tag = "t:" + target.Id, Background = Brushes.Transparent, Width = 32, Height = 32, IsHitTestVisible = true };
        Panel.SetZIndex(b, 10);
        return b;
    }

    private static string GetTargetIconFile(string type, string side) => type switch { "smoke" => "smoke.png", "flash" => "flash.png", "he" => "he.png", "molotov" => side == "CT" ? "incendiary.png" : "molotov.png", "wallbang" => "", "jump" => "", _ => "smoke.png" };

    private void DrawHighlight(Guid targetId)
    {
        foreach (var c in MapCanvas.Children.OfType<FrameworkElement>().Where(c => c.Tag?.ToString() is string t && (t.StartsWith("l:") || t == "dash" || t == "glow")).ToList()) MapCanvas.Children.Remove(c);
        var target = _mapTargets.FirstOrDefault(t => t.Id == targetId); if (target == null) return;
        // Add orange border highlight to the target icon
        var targetIcon = MapCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag?.ToString() == "t:" + targetId);
        if (targetIcon != null) targetIcon.BorderBrush = new SolidColorBrush(Color.FromRgb(0xf5, 0xa6, 0x23));
        
        var showLineups = (_proOnly ? target.Lineups.Where(l => l.IsPro) : target.Lineups).OrderBy(l => l.Sequence).ToList();

        // Group by GroupId for icon display (only show one icon per group, with badge)
        var groupedIcons = new Dictionary<Guid, (LineupEntity first, int count)>();
        foreach (var l in showLineups)
        {
            var key = l.GroupId ?? l.Id;
            if (groupedIcons.ContainsKey(key))
                groupedIcons[key] = (groupedIcons[key].first, groupedIcons[key].count + 1);
            else
                groupedIcons[key] = (l, 1);
        }

        foreach (var l in showLineups)
        {
            var key = l.GroupId ?? l.Id;
            bool isGroupRep = groupedIcons.ContainsKey(key) && groupedIcons[key].first.Id == l.Id;

            // Dashed line from target border (radius 14) toward lineup
            double dx = l.X - target.X, dy = l.Y - target.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < 0.01) dist = 1;
            double nx = dx / dist, ny = dy / dist;
            double startX = target.X + nx * 14, startY = target.Y + ny * 14;
            MapCanvas.Children.Add(new Line { X1 = startX, Y1 = startY, X2 = l.X, Y2 = l.Y, Stroke = new SolidColorBrush(Color.FromRgb(0xf5, 0xa6, 0x23)) { Opacity = 0.7 }, StrokeThickness = 2, StrokeDashArray = new DoubleCollection { 4, 3 }, Tag = "dash" });

            // Only draw icon for the group representative
            if (isGroupRep)
            {
                int variantCount = groupedIcons[key].count;
                var ic = CreateLineupIcon(l, variantCount);
                Canvas.SetLeft(ic, l.X - 12); Canvas.SetTop(ic, l.Y - 12);
                ic.Tag = "l:" + key; // Tag by GroupId
                // Add all member IDs for hit testing
                MapCanvas.Children.Add(ic);
            }
        }
    }

    // ===== TRICKS =====
    private void DrawTricks()
    {
        if (_activeType is "wallbang" or "jump")
        {
            // Use Target+Lineup model
            var visible = _mapTargets.Where(t => t.Type == _activeType && (_activeSide == "Both" || t.Side == _activeSide) && t.Floor == _activeFloor && (!_proOnly || t.Lineups.Any(l => l.IsPro))).ToList();
            ClearMapOverlays();
            foreach (var t in visible) { var i = CreateTrickTargetIcon(t); Canvas.SetLeft(i, t.X - 16); Canvas.SetTop(i, t.Y - 16); MapCanvas.Children.Add(i); }
            if (_highlightedTargetId.HasValue && visible.Any(t => t.Id == _highlightedTargetId.Value)) DrawHighlight(_highlightedTargetId.Value);
            else _highlightedTargetId = null;
            _highlightedTrickId = null;
        }
        else
        {
            // Boost/Camp: standalone TrickEntity
            bool filterSide = _activeType is "boost";
            var visible = _mapTricks.Where(t => t.Type == _activeType && (!filterSide || _activeSide == "Both" || t.Side == _activeSide)).ToList();
            ClearMapOverlays();
            foreach (var t in visible) { var i = CreateTrickIcon(t); Canvas.SetLeft(i, t.X - 16); Canvas.SetTop(i, t.Y - 16); MapCanvas.Children.Add(i); }
            _highlightedTrickId = null;
            _highlightedTargetId = null;
        }
    }

    private Border CreateTrickIcon(TrickEntity trick)
    {
        var g = new Grid();
        var color = trick.Type switch { "wallbang" => Color.FromRgb(0xe7, 0x4c, 0x3c), "boost" => Color.FromRgb(0x34, 0x95, 0xdb), "jump" => Color.FromRgb(0x2e, 0xcc, 0x71), "camp" => Color.FromRgb(0xf3, 0x9c, 0x12), _ => Color.FromRgb(0x88, 0x88, 0x88) };
        g.Children.Add(new Ellipse { Width = 32, Height = 32, Fill = new SolidColorBrush(color), Stroke = new SolidColorBrush(Color.FromRgb(0x1a, 0x1d, 0x24)), StrokeThickness = 2 });
        var label = trick.Type switch { "wallbang" => "W", "boost" => "B", "jump" => "J", "camp" => "C", _ => "?" };
        g.Children.Add(new TextBlock { Text = label, FontSize = 12, FontWeight = FontWeights.Bold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
        var border = new Border { Child = g, Tag = "tr:" + trick.Id, Background = Brushes.Transparent, Width = 32, Height = 32, IsHitTestVisible = true };
        return border;
    }

    private void ShowTrickDetail(TrickEntity trick)
    {
        new TrickDetailWindow(trick) { Owner = Window.GetWindow(this) }.ShowDialog();
    }



    private Point ScreenToCanvas(Point s) => new((s.X - MapTranslate.X) / _zoom, (s.Y - MapTranslate.Y) / _zoom);
    private static double Dist(Point p, double x, double y) => Math.Sqrt((p.X - x) * (p.X - x) + (p.Y - y) * (p.Y - y));

    private (TargetEntity? t, bool isL, LineupEntity? l, List<LineupEntity>? siblings) HitTestAll(Point cpt)
    {
        if (_highlightedTargetId.HasValue) { var ht = _mapTargets.FirstOrDefault(x => x.Id == _highlightedTargetId.Value); if (ht != null) foreach (var l in ht.Lineups.OrderBy(x => x.Sequence)) if (Dist(cpt, l.X, l.Y) < 16) { var sibs = l.GroupId != null ? ht.Lineups.Where(x => x.GroupId == l.GroupId).OrderBy(x => x.Sequence).ToList() : null; return (ht, true, l, sibs); } }
        foreach (var t in _mapTargets.Where(x => x.Type == _activeType && (_activeSide == "Both" || x.Side == _activeSide) && x.Floor == _activeFloor)) if (Dist(cpt, t.X, t.Y) < 25) return (t, false, null, null);
        return (null, false, null, null);
    }

    private TrickEntity? HitTestTrick(Point cpt)
    {
        bool wallbangBoost = _activeType is "wallbang" or "boost";
        foreach (var t in _mapTricks.Where(x => x.Type == _activeType && (!wallbangBoost || _activeSide == "Both" || x.Side == _activeSide))) if (Dist(cpt, t.X, t.Y) < 20) return t;
        return null;
    }

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

    private void Map_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_minZoom < 0.001) TryDelayedInit();
        var mp = e.GetPosition(MapBorder); var oc = ScreenToCanvas(mp);
        _zoom = Math.Max(_minZoom, Math.Min(5.0, _zoom + (e.Delta > 0 ? 0.15 : -0.15)));
        ApplyZoom();
        var nc = ScreenToCanvas(mp); MapTranslate.X += (nc.X - oc.X) * _zoom; MapTranslate.Y += (nc.Y - oc.Y) * _zoom;
        ClampMap();
    }

    private void Map_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_positionPickActive) return;
        if (_minZoom < 0.001) TryDelayedInit();
        _isPanning = true; _didDrag = false; _panStart = e.GetPosition(MapBorder);
        _translateBeforePan = new Point(MapTranslate.X, MapTranslate.Y);
        MapBorder.CaptureMouse(); RemovePreviewMarker();
    }

    private void Map_MouseMove(object sender, MouseEventArgs e)
    {
        if (_positionPickActive && _positionPickMarker != null)
        {
            var cp = ScreenToCanvas(e.GetPosition(MapBorder));
            Canvas.SetLeft(_positionPickMarker, cp.X - 13);
            Canvas.SetTop(_positionPickMarker, cp.Y - 13);
            return;
        }
        if (!_isPanning) return;
        var p = e.GetPosition(MapBorder); MapTranslate.X = _translateBeforePan.X + (p.X - _panStart.X); MapTranslate.Y = _translateBeforePan.Y + (p.Y - _panStart.Y);
        if (Math.Abs(p.X - _panStart.X) > 3 || Math.Abs(p.Y - _panStart.Y) > 3) _didDrag = true;
        ClampMap();
    }

    private void Map_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_positionPickActive)
        {
            var cp = ScreenToCanvas(e.GetPosition(MapBorder));
            CompletePositionPick(cp.X, cp.Y);
            return;
        }
        _isPanning = false; MapBorder.ReleaseMouseCapture(); ClampMap();
        if (_didDrag) return;
        var cpt = ScreenToCanvas(e.GetPosition(MapBorder));

        if (_mode == "tricks")
        {
            if (_activeType is "wallbang" or "jump")
            {
                // Use Target+Lineup interaction
                var (tt, isLL, ll, sibs) = HitTestAll(cpt);
                if (isLL && ll != null && tt != null) {
                if (sibs != null && sibs.Count > 1)
                    ShowLineupVariantPicker(sibs, tt);
                else
                    new LineupDetailWindow(ll, tt) { Owner = Window.GetWindow(this) }.ShowDialog();
                return;
            }
                if (tt != null) { if (_highlightedTargetId == tt.Id) { _highlightedTargetId = null; DrawTricks(); return; } _highlightedTargetId = tt.Id; DrawHighlight(tt.Id); return; }
                if (_highlightedTargetId.HasValue) { _highlightedTargetId = null; DrawTricks(); }
            }
            else
            {
                var trick = HitTestTrick(cpt);
                if (trick != null) { ShowTrickDetail(trick); return; }
                if (_highlightedTrickId.HasValue) { _highlightedTrickId = null; DrawTricks(); }
            }
            return;
        }

        // Nades mode
        var (t, isL, l, siblings) = HitTestAll(cpt);
        if (isL && l != null && t != null) {
            if (siblings != null && siblings.Count > 1)
                ShowLineupVariantPicker(siblings, t);
            else
                new LineupDetailWindow(l, t) { Owner = Window.GetWindow(this) }.ShowDialog();
            return;
        }
        if (t != null) { if (_highlightedTargetId == t.Id) { _highlightedTargetId = null; DrawTargets(); return; } _highlightedTargetId = t.Id; DrawHighlight(t.Id); return; }
        if (_highlightedTargetId.HasValue) { _highlightedTargetId = null; DrawTargets(); }
    }

    private void Map_RightClick(object sender, MouseButtonEventArgs e)
    {
        var cpt = ScreenToCanvas(e.GetPosition(MapBorder));

        if (_mode == "tricks")
        {
            var m = new ContextMenu();
            if (_activeType is "wallbang" or "jump")
            {
                var (tt, isLL, ll, sibs) = HitTestAll(cpt);
                var terms = GetTrickTerms();
                if (isLL && ll != null && tt != null) { ShowPreviewMarker(ll.X, ll.Y); m.Items.Add(Mi(Loc.Get("edit") + " " + terms.lineupLabel, () => EditLineupT(tt, ll))); m.Items.Add(Mi(Loc.Get("delete") + " " + terms.lineupLabel, () => DeleteLineupT(tt, ll))); m.Items.Add(new Separator()); }
                else if (tt != null) { ShowPreviewMarker(tt.X, tt.Y); var ttid = tt.Id; m.Items.Add(Mi(Loc.Get("edit") + " " + terms.targetLabel, () => EditTargetT(tt))); m.Items.Add(Mi(Loc.Get("delete") + " " + terms.targetLabel, () => DeleteTarget(ttid))); m.Items.Add(Mi(terms.addLineupLabel, () => AddLineupToTarget(ttid))); m.Items.Add(new Separator()); }
                else { ShowPreviewMarker(cpt.X, cpt.Y); }
                m.Items.Add(Mi(terms.addTargetLabel, () => OpenCreateTrickSpotAt(cpt.X, cpt.Y)));
            }
            else
            {
                var trick = HitTestTrick(cpt);
                if (trick != null) { ShowPreviewMarker(trick.X, trick.Y); m.Items.Add(Mi(Loc.Get("edit") + " " + trick.Name, () => EditTrickT(trick))); m.Items.Add(Mi(Loc.Get("delete") + " " + trick.Name, () => DeleteTrick(trick.Id))); m.Items.Add(new Separator()); }
                m.Items.Add(Mi(Loc.Get("map.add_target"), () => OpenCreateTrickSpotAt(cpt.X, cpt.Y)));
            }
            m.PlacementTarget = sender as UIElement; m.Closed += (_, _) => RemovePreviewMarker(); m.IsOpen = true;
            return;
        }

        // Nades mode
        var (t, isL, l, siblings) = HitTestAll(cpt);
        var cm = new ContextMenu();
        if (isL && l != null && t != null) { ShowPreviewMarker(l.X, l.Y); cm.Items.Add(Mi(Loc.Get("map.edit_lineup"), () => EditLineupT(t, l))); cm.Items.Add(Mi(Loc.Get("map.delete_lineup"), () => DeleteLineupT(t, l))); cm.Items.Add(new Separator()); }
        else if (t != null) { ShowPreviewMarker(t.X, t.Y); var tid = t.Id; cm.Items.Add(Mi(Loc.Get("map.edit_target"), () => EditTargetT(t))); cm.Items.Add(Mi(Loc.F("map.delete_target", t.Name), () => DeleteTarget(tid))); cm.Items.Add(Mi(Loc.Get("map.add_lineup"), () => AddLineupToTarget(tid))); cm.Items.Add(new Separator()); }
        else { ShowPreviewMarker(cpt.X, cpt.Y); }
        cm.Items.Add(Mi(Loc.Get("map.add_target"), () => OpenCreateTargetAt(cpt.X, cpt.Y)));
        cm.PlacementTarget = sender as UIElement; cm.Closed += (_, _) => RemovePreviewMarker(); cm.IsOpen = true;
    }

    private string L(string key) => Loc.Get(key);
    // Get localized terms based on current trick type for wallbang/jump
    private (string targetLabel, string lineupLabel, string addTargetLabel, string addLineupLabel) GetTrickTerms()
    {
        return _activeType switch
        {
            "wallbang" => (L("wallbang_target"), L("wallbang_lineup"), L("wallbang_add_target"), L("wallbang_add_lineup")),
            "jump" => (L("jump_target"), L("jump_lineup"), L("jump_add_target"), L("jump_add_lineup")),
            _ => (Loc.Get("map.add_target"), Loc.Get("map.add_lineup"), Loc.Get("map.add_target"), Loc.Get("map.add_lineup"))
        };
    }

    private static MenuItem Mi(string h, Action a) { var i = new MenuItem { Header = h }; i.Click += (_, _) => a(); return i; }

    // ===== Nades creation =====
    public void OpenCreateTargetAt(double x, double y)
    {
        // Check if there's a nearby existing target of the same type/side/floor to merge into
        var settings = SettingsService.Load();
        double threshold = settings.TargetConflictRadius;
        var nearbyTarget = _mapTargets.FirstOrDefault(t =>
            t.Type == _activeType && t.Floor == _activeFloor &&
            (_activeSide == "Both" || t.Side == _activeSide) &&
            Dist(new Point(t.X, t.Y), x, y) < threshold);

        if (nearbyTarget != null && _dataService != null)
        {
            // Merge: skip CreateTargetWindow, go straight to AddLineupWindow for this existing target
            var msg = Loc.F("nearby.msg", nearbyTarget.Name);
            if (MessageBox.Show(msg, Loc.Get("nearby.title"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                AddLineupToTarget(nearbyTarget.Id);
            }
            return;
        }

        ShowPreviewMarker(x, y);
        var w = new CreateTargetWindow(x, y, target => { RemovePreviewMarker(); if (_dataService == null || _activeProfile == null) return; target.ProfileId = _activeProfile.Id; target.MapId = _mapId; target.Floor = _activeFloor; _dataService.AddTarget(target);
            Dispatcher.BeginInvoke(new Action(() => {
                var lw = new AddLineupWindow(x + 100, y + 50) { Owner = Window.GetWindow(this) };
                ShowLineupWindowWithPick(lw, finalLw => { if (finalLw.Confirmed) { _dataService.AddLineup(new LineupEntity { TargetId = target.Id, Name = finalLw.LineupNameValue, Side = finalLw.SideValue, Sequence = 1, X = finalLw.X, Y = finalLw.Y, Floor = _activeFloor, AimDescription = finalLw.AimDescription, ThrowType = finalLw.ThrowTypeValue, VideoUrl = finalLw.VideoUrlValue, Notes = finalLw.NotesValue, ImagesJson = finalLw.ImagesJson, IsDefault = false, IsPro = finalLw.IsPro, CreatedAt = DateTime.UtcNow }); } else { _dataService.DeleteTarget(target.Id); _dataService.SaveChanges(); } _highlightedTargetId = null; ReloadTargets(); });
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }) { Owner = Window.GetWindow(this) };
        w.Closed += (_, _) => RemovePreviewMarker(); w.ShowDialog();
    }

    private void ShowLineupVariantPicker(List<LineupEntity> siblings, TargetEntity target)
    {
        var win = new Window
        {
            Title = Loc.Get("lineup.variants"),
            Width = 340, Height = 200 + siblings.Count * 30,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Window.GetWindow(this),
            Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x1d, 0x24)),
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true
        };
        var border = new Border { Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x1d, 0x24)), CornerRadius = new CornerRadius(12), BorderBrush = new SolidColorBrush(Color.FromRgb(0x2a, 0x2d, 0x34)), BorderThickness = new Thickness(1) };
        var sp = new StackPanel { Margin = new Thickness(20) };

        sp.Children.Add(new TextBlock { Text = Loc.Get("lineup.variants"), FontSize = 14, FontWeight = FontWeights.Bold, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 10) });

        foreach (var s in siblings)
        {
            var btn = new Button
            {
                Content = "#" + s.Sequence + " " + (s.AimDescription ?? ""),
                Background = new SolidColorBrush(Color.FromRgb(0x2a, 0x2d, 0x34)),
                Foreground = new SolidColorBrush(Color.FromRgb(0xcc, 0xcc, 0xcc)),
                BorderThickness = new Thickness(0),
                Height = 32, Margin = new Thickness(0, 0, 0, 4),
                FontSize = 13, HorizontalContentAlignment = HorizontalAlignment.Left,
                Cursor = Cursors.Hand,
                Tag = s
            };
            btn.Click += (sb, ev) =>
            {
                win.Close();
                if (sb is Button b && b.Tag is LineupEntity sel)
                    new LineupDetailWindow(sel, target, siblings: siblings) { Owner = Window.GetWindow(this) }.ShowDialog();
            };
            sp.Children.Add(btn);
        }
        border.Child = sp;
        win.Content = border;
        win.ShowDialog();
    }


    private void DeleteTarget(Guid id)
    {
        if (_dataService == null) return; var t = _dataService.GetTarget(id); if (t == null) return;
        if (t.IsDefault && _activeProfile != null && !_activeProfile.AllowDeleteDefaultSpots) { MessageBox.Show(Loc.Get("map.protected"), Loc.Get("map.protected_title"), MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (MessageBox.Show(Loc.F("map.delete_confirm", t.Name, t.Lineups.Count), Loc.Get("map.delete_title"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        _dataService.DeleteTarget(t.Id);
        _mapTargets.RemoveAll(x => x.Id == id);
        _highlightedTargetId = null;
        // Remove just this target's UI elements from canvas (no full redraw)
        foreach (var c in MapCanvas.Children.OfType<FrameworkElement>().Where(c => (c.Tag?.ToString() ?? "") == "t:" + id).ToList())
            MapCanvas.Children.Remove(c);
        // Also remove any dash lines and glow for this target
        foreach (var c in MapCanvas.Children.OfType<FrameworkElement>().Where(c => (c.Tag?.ToString() ?? "") is "dash" or "glow").ToList())
            MapCanvas.Children.Remove(c);
        foreach (var c in MapCanvas.Children.OfType<FrameworkElement>().Where(c => (c.Tag?.ToString() ?? "").StartsWith("l:")).ToList())
            MapCanvas.Children.Remove(c);
        RemovePreviewMarker();
    }

    private void EditTargetT(TargetEntity t)
    {
        if (_dataService == null) return;
        if (t.IsDefault && _activeProfile != null && !_activeProfile.AllowDeleteDefaultSpots) { MessageBox.Show(Loc.Get("map.protected"), Loc.Get("map.protected_title"), MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        var fresh = _dataService.GetTarget(t.Id);
        if (fresh == null) return;
        var w = new CreateTargetWindow(fresh.X, fresh.Y, target => {
            fresh.Name = target.Name; fresh.Type = target.Type; fresh.Side = target.Side;
            fresh.X = target.X; fresh.Y = target.Y; fresh.Image = target.Image;
            _dataService.SaveChanges(); ReloadTargets();
        }) { Owner = Window.GetWindow(this) };
        w.SetExistingValues(t.Name, t.Type, t.Side, t.X, t.Y, t.Image);
        if (t.Type == "wallbang" || t.Type == "jump") w.SetTrickContext(t.Type);
        w.ShowDialog();
    }

    private void DeleteLineupT(TargetEntity t, LineupEntity l)
    {
        if (_dataService == null) return;
        if (t.Lineups.Count <= 1) { MessageBox.Show(Loc.Get("map.min_lineup"), Loc.Get("map.cannot_delete"), MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (l.IsDefault && _activeProfile != null && !_activeProfile.AllowDeleteDefaultSpots) { MessageBox.Show(Loc.Get("map.protected"), Loc.Get("map.protected_title"), MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (MessageBox.Show("Delete lineup #" + l.Sequence + "?", "Delete Lineup", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        var fresh = _dataService.GetLineup(l.Id);
        if (fresh == null) return;
        _dataService.DeleteLineup(fresh.Id);
        var remaining = _dataService.GetLineupsQuery(t.Id);
        for (int i = 0; i < remaining.Count; i++) remaining[i].Sequence = i + 1;
        _dataService.SaveChanges();
        ReloadTargets();
    }

    private void EditLineupT(TargetEntity t, LineupEntity l)
    {
        if (_dataService == null) return;
        if (l.IsDefault && _activeProfile != null && !_activeProfile.AllowDeleteDefaultSpots) { MessageBox.Show(Loc.Get("map.protected"), Loc.Get("map.protected_title"), MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        var lw = new AddLineupWindow(l.X, l.Y) { Owner = Window.GetWindow(this) };
        lw.PreFillFull(l.Name ?? "", l.Side ?? "T", l.AimDescription ?? "", l.ThrowType ?? "standing", l.VideoUrl ?? "", l.Notes ?? "");
        lw.IsPro = l.IsPro;
        lw.ProCheck.IsChecked = l.IsPro;
        try { var paths = System.Text.Json.JsonSerializer.Deserialize<List<string>>(l.ImagesJson ?? "[]"); if (paths != null) lw.SetExistingImages(paths); } catch { }
        ShowLineupWindowWithPick(lw, finalLw => {
            if (finalLw.Confirmed)
            {
                var settings = SettingsService.Load();
                double threshold = settings.LineupConflictRadius;
                // Check if moved to another lineup's position (excluding self)
                var nearby = t.Lineups.FirstOrDefault(el => el.Id != l.Id && Dist(new Point(el.X, el.Y), finalLw.X, finalLw.Y) < threshold);
                if (nearby != null)
                {
                    var result = MessageBox.Show(
                        "Lineup #" + nearby.Sequence + " is already at this position. Overwrite it and remove this one?",
                        "Lineup Variant", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        var freshNearby = _dataService.GetLineup(nearby.Id);
                        if (freshNearby != null)
                        {
                            freshNearby.X = finalLw.X; freshNearby.Y = finalLw.Y;
                            freshNearby.Name = finalLw.LineupNameValue; freshNearby.Side = finalLw.SideValue;
                            freshNearby.AimDescription = finalLw.AimDescription;
                            freshNearby.ThrowType = finalLw.ThrowTypeValue;
                            freshNearby.VideoUrl = finalLw.VideoUrlValue;
                            freshNearby.Notes = finalLw.NotesValue;
                            freshNearby.ImagesJson = finalLw.ImagesJson;
                            freshNearby.IsPro = finalLw.IsPro;
                            // Remove the old lineup
                            var oldFresh = _dataService.GetLineup(l.Id);
                            if (oldFresh != null) _dataService.DeleteLineup(oldFresh.Id);
                            // Re-sequence
                            var remaining = _dataService.GetLineupsQuery(t.Id);
                            for (int i = 0; i < remaining.Count; i++) remaining[i].Sequence = i + 1;
                            _dataService.SaveChanges();
                        }
                    }
                }
                else
                {
                    var fresh = _dataService.GetLineup(l.Id);
                    if (fresh != null) {
                        fresh.X = finalLw.X; fresh.Y = finalLw.Y;
                        fresh.Name = finalLw.LineupNameValue; fresh.Side = finalLw.SideValue;
                        fresh.AimDescription = finalLw.AimDescription;
                        fresh.ThrowType = finalLw.ThrowTypeValue;
                        fresh.VideoUrl = finalLw.VideoUrlValue;
                        fresh.Notes = finalLw.NotesValue;
                        fresh.ImagesJson = finalLw.ImagesJson;
                        fresh.IsPro = finalLw.IsPro;
                        _dataService.SaveChanges();
                    }
                }
            }
            ReloadTargets();
        });
    }

    private void AddLineupToTarget(Guid tid)
    {
        var t = _mapTargets.FirstOrDefault(x => x.Id == tid); if (t == null || _dataService == null) return;
        var lw = new AddLineupWindow(t.X + 100, t.Y + 50) { Owner = Window.GetWindow(this) };
        ShowLineupWindowWithPick(lw, finalLw => {
            if (finalLw.Confirmed)
            {
                var settings = SettingsService.Load();
                double threshold = settings.LineupConflictRadius;
                // Check for nearby existing lineup of the SAME target to merge with
                var nearbyLineup = t.Lineups.FirstOrDefault(el => Dist(new Point(el.X, el.Y), finalLw.X, finalLw.Y) < threshold);
                if (nearbyLineup != null)
                {
                    var result = MessageBox.Show(
                        "A lineup (#" + nearbyLineup.Sequence + ") already exists at this position. Create as a new variant (same position, different setup)?",
                        "Lineup Variant", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        var fresh = _dataService.GetLineup(nearbyLineup.Id);
                        if (fresh != null)
                        {
                            fresh.X = finalLw.X; fresh.Y = finalLw.Y;
                            fresh.AimDescription = finalLw.AimDescription;
                            fresh.ThrowType = finalLw.ThrowTypeValue;
                            fresh.VideoUrl = finalLw.VideoUrlValue;
                            fresh.Notes = finalLw.NotesValue;
                            fresh.ImagesJson = finalLw.ImagesJson;
                            _dataService.SaveChanges();
                        }
                    }
                }
                else
                {
                    int ms = t.Lineups.Select(x => (int?)x.Sequence).Max() ?? 0;
                    _dataService.AddLineup(new LineupEntity { TargetId = tid, Name = finalLw.LineupNameValue, Side = finalLw.SideValue, Sequence = ms + 1, X = finalLw.X, Y = finalLw.Y, Floor = _activeFloor, AimDescription = finalLw.AimDescription, ThrowType = finalLw.ThrowTypeValue, VideoUrl = finalLw.VideoUrlValue, Notes = finalLw.NotesValue, ImagesJson = finalLw.ImagesJson, IsDefault = false, IsPro = finalLw.IsPro, CreatedAt = DateTime.UtcNow });
                    _dataService.SaveChanges();
                   
                   
                }
            }
            ReloadTargets();
        });
    }

    // ===== Tricks creation =====
    public void OpenCreateTrickAt(double x, double y)
    {
        ShowPreviewMarker(x, y);
        var w = new CreateTrickWindow(x, y, trick => { RemovePreviewMarker(); if (_dataService == null || _activeProfile == null) return; trick.ProfileId = _activeProfile.Id; trick.MapId = _mapId; trick.Floor = _activeFloor; trick.Type = trick.Type; _dataService.AddTrick(trick); ReloadTricks(); }) { Owner = Window.GetWindow(this) };
        w.PreSelectType(_activeType);
        w.Closed += (_, _) => RemovePreviewMarker(); w.ShowDialog();
    }

    // Unified trick spot creation - uses CreateTrickWindow with default type
    public void OpenCreateTrickSpotAt(double x, double y)
    {
        ShowPreviewMarker(x, y);
        var defType = SettingsService.Load().DefaultTrickType;
        var w = new CreateTrickWindow(x, y, trick => {
            RemovePreviewMarker();
            if (_dataService == null || _activeProfile == null) return;
            trick.ProfileId = _activeProfile.Id;
            trick.MapId = _mapId;
            trick.Floor = _activeFloor;

            if (trick.Type is "wallbang" or "jump")
            {
                var target = new TargetEntity
                {
                    ProfileId = _activeProfile.Id, MapId = _mapId,
                    Name = trick.Name, Type = trick.Type,
                    Side = trick.Side ?? "Both",
                    X = trick.X, Y = trick.Y, Floor = _activeFloor,
                    IsDefault = false, CreatedAt = DateTime.UtcNow
                };
                _dataService.AddTarget(target);

                // Defer showing lineup window until after CreateTrickWindow is fully closed
                Dispatcher.BeginInvoke(new Action(() => {
                    var lw = new AddLineupWindow(x + 100, y + 50) { Owner = Window.GetWindow(this) };
                    ShowLineupWindowWithPick(lw, finalLw => {
                        if (finalLw.Confirmed)
                        {
                            _dataService.AddLineup(new LineupEntity {
                                TargetId = target.Id, Name = finalLw.LineupNameValue, Side = finalLw.SideValue, Sequence = 1,
                                X = finalLw.X, Y = finalLw.Y, Floor = _activeFloor,
                                AimDescription = finalLw.AimDescription,
                                ThrowType = finalLw.ThrowTypeValue,
                                VideoUrl = finalLw.VideoUrlValue,
                                Notes = finalLw.NotesValue,
                                ImagesJson = finalLw.ImagesJson,
                                IsDefault = false, IsPro = finalLw.IsPro, CreatedAt = DateTime.UtcNow
                            });
                           
                        }
                        else
                        {
                            var t = _dataService.GetTarget(target.Id);
                            if (t != null) { _dataService.DeleteTarget(t.Id); _dataService.SaveChanges(); }
                        }
                        _highlightedTargetId = null;
                        ReloadTricks();
                    });
                }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            }
            else
            {
                _dataService.AddTrick(trick);
                ReloadTricks();
            }
        }) { Owner = Window.GetWindow(this) };
        w.PreSelectType(defType);
        w.Closed += (_, _) => RemovePreviewMarker();
        w.ShowDialog();
    }

    // Wallbang/Jump: create as Target+Lineup (stored in Targets table with Type = "wallbang" or "jump")
    public void OpenCreateTrickTargetAt(double x, double y)
    {
        ShowPreviewMarker(x, y);
        // Build a factory with the trick type pre-set
        var trickType = _activeType; // "wallbang" or "jump"
        var trickSide = trickType == "jump" ? "Both" : (_activeSide == "Both" ? "T" : _activeSide);
        var w = new CreateTargetWindow(x, y, target => {
            RemovePreviewMarker();
            if (_dataService == null || _activeProfile == null) return;
            target.ProfileId = _activeProfile.Id;
            target.MapId = _mapId;
            target.Floor = _activeFloor;
            target.Type = trickType;
            target.Side = trickSide;
            _dataService.AddTarget(target);
            var lw = new AddLineupWindow(x + 100, y + 50) { Owner = Window.GetWindow(this) };
            ShowLineupWindowWithPick(lw, finalLw => {
                if (finalLw.Confirmed) {
                    _dataService.AddLineup(new LineupEntity { TargetId = target.Id, Name = finalLw.LineupNameValue, Side = finalLw.SideValue, Sequence = 1, X = finalLw.X, Y = finalLw.Y, Floor = _activeFloor, AimDescription = finalLw.AimDescription, ThrowType = finalLw.ThrowTypeValue, VideoUrl = finalLw.VideoUrlValue, Notes = finalLw.NotesValue, ImagesJson = finalLw.ImagesJson, IsDefault = false, IsPro = finalLw.IsPro, CreatedAt = DateTime.UtcNow });
                   
                }
                _highlightedTargetId = null;
                ReloadTricks();
            });
        }) { Owner = Window.GetWindow(this) };
        w.SetTrickContext(trickType);
        w.Closed += (_, _) => RemovePreviewMarker();
        w.ShowDialog();
    }

    private void EditTrickT(TrickEntity trick)
    {
        if (_dataService == null) return;
        if (trick.IsDefault && _activeProfile != null && !_activeProfile.AllowDeleteDefaultSpots) { MessageBox.Show(Loc.Get("map.protected"), Loc.Get("map.protected_title"), MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        var fresh = _dataService.GetTrick(trick.Id);
        if (fresh == null) return;
       var w = new CreateTrickWindow(fresh.X, fresh.Y, updated => {
           fresh.Name = updated.Name; fresh.Type = updated.Type; fresh.Side = updated.Side;
           fresh.X = updated.X; fresh.Y = updated.Y; fresh.VideoUrl = updated.VideoUrl;
            fresh.Notes = updated.Notes; fresh.ImagesJson = updated.ImagesJson;
           _dataService.SaveChanges(); ReloadTricks();
       }) { Owner = Window.GetWindow(this) };
        w.SetExistingValues(trick.Name, trick.Type, trick.Side ?? "", trick.X, trick.Y, trick.VideoUrl, trick.Notes, trick.ImagesJson);
        w.ShowDialog();
    }

    private void DeleteTrick(Guid id)
    {
        if (_dataService == null) return; var t = _dataService.GetTrick(id); if (t == null) return;
        if (t.IsDefault && _activeProfile != null && !_activeProfile.AllowDeleteDefaultSpots) { MessageBox.Show(Loc.Get("map.protected"), Loc.Get("map.protected_title"), MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (MessageBox.Show(Loc.F("map.delete_confirm", t.Name, 0), Loc.Get("map.delete_title"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        _dataService.DeleteTrick(t.Id); _dataService.SaveChanges();
        ReloadTricks();
    }

    // ===== Nades filter handlers =====
    private static void SetBorderBg(Border border, Brush bg) => border.Background = bg;
    private static void SetTextFg(TextBlock tb, Brush fg) => tb.Foreground = fg;

    private void Filter_Click(object sender, MouseButtonEventArgs e) { var tag = ((FrameworkElement)sender).Tag?.ToString(); if (tag != null) { _activeType = tag; DrawTargets(); UpdateFilterHighlights(); } }
    private void Side_Click(object sender, MouseButtonEventArgs e) { var tag = ((FrameworkElement)sender).Tag?.ToString(); if (tag != null) { _activeSide = tag; DrawTargets(); UpdateFilterHighlights(); } }
    private void Floor_Click(object sender, MouseButtonEventArgs e) { var tag = ((FrameworkElement)sender).Tag?.ToString(); if (tag != null) { _activeFloor = tag; SwitchFloorRadar(); if (_mode == "nades") { DrawTargets(); UpdateFilterHighlights(); } else { ReloadTricks(); UpdateTrickFilterHighlights(); } } }

    private void SwitchFloorRadar()
    {
        if (_mapInfo == null || !_mapInfo.HasLowerFloor) return;
        var assetsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");
        var radarPath = _activeFloor == "lower" && !string.IsNullOrEmpty(_mapInfo.LowerRadarPath)
            ? System.IO.Path.Combine(assetsPath, _mapInfo.LowerRadarPath)
            : System.IO.Path.Combine(assetsPath, _mapInfo.RadarPath);
        if (System.IO.File.Exists(radarPath))
        {
            var bmp = new BitmapImage(); bmp.BeginInit(); bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(radarPath); bmp.EndInit(); bmp.Freeze();
            MapImage.Source = bmp;
        }
        // Reset zoom/pan to fit new image
        _minZoom = 0;
        TryDelayedInit();
    }

    private void UpdateFilterHighlights()
    {
        var selBg = new SolidColorBrush(Color.FromRgb(0x3a, 0x4a, 0x18));
        var actBg = new SolidColorBrush(Color.FromRgb(0xf5, 0xa6, 0x23));
        var defBg = new SolidColorBrush(Color.FromRgb(0x1a, 0x1d, 0x24));
        var gr = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
        var dk = new SolidColorBrush(Color.FromRgb(0x0f, 0x11, 0x16));
        SetBorderBg(SmokePill, _activeType == "smoke" ? selBg : defBg);
        SetBorderBg(FlashPill, _activeType == "flash" ? selBg : defBg);
        SetBorderBg(MollyPill, _activeType == "molotov" ? selBg : defBg);
        SetBorderBg(HEPill, _activeType == "he" ? selBg : defBg);
        SetBorderBg(ProPill, _proOnly ? selBg : defBg);
        SetTextFg(ProPillText, _proOnly ? dk : gr);
        SetBorderBg(TPill, _activeSide == "T" ? actBg : defBg);
        SetBorderBg(CTPill, _activeSide == "CT" ? actBg : defBg);
        SetBorderBg(BothPill, _activeSide == "Both" ? actBg : defBg);
        SetBorderBg(UpperPill, _activeFloor == "default" ? actBg : defBg);
        SetBorderBg(LowerPill, _activeFloor == "lower" ? actBg : defBg);
        SetTextFg(FilterT, _activeSide == "T" ? dk : gr);
        SetTextFg(FilterCT, _activeSide == "CT" ? dk : gr);
        SetTextFg(FilterBoth, _activeSide == "Both" ? dk : gr);
        SetTextFg(FloorDefault, _activeFloor == "default" ? dk : gr);
        SetTextFg(FloorLower, _activeFloor == "lower" ? dk : gr);
        SetBorderBg(UpperPill, _activeFloor == "default" ? actBg : defBg);
        SetBorderBg(LowerPill, _activeFloor == "lower" ? actBg : defBg);
    }

    // ===== Tricks filter handlers =====
    private void TrickType_Click(object sender, MouseButtonEventArgs e) { var tag = ((FrameworkElement)sender).Tag?.ToString(); if (tag != null) { _activeType = tag; if (tag is "jump" or "camp") _activeSide = "Both"; if (tag is "wallbang" or "jump") ReloadTricks(); else { ClearMapOverlays(); DrawTricks(); } UpdateTrickFilterHighlights(); } }
    private void TrickSide_Click(object sender, MouseButtonEventArgs e) { var tag = ((FrameworkElement)sender).Tag?.ToString(); if (tag != null) { _activeSide = tag; if (_activeType is "wallbang" or "jump") ReloadTricks(); else DrawTricks(); UpdateTrickFilterHighlights(); } }

    private void UpdateTrickFilterHighlights()
    {
        var selBg = new SolidColorBrush(Color.FromRgb(0x3a, 0x4a, 0x18));
        var actBg = new SolidColorBrush(Color.FromRgb(0xf5, 0xa6, 0x23));
        var defBg = new SolidColorBrush(Color.FromRgb(0x1a, 0x1d, 0x24));
        var gr = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
        var dk = new SolidColorBrush(Color.FromRgb(0x0f, 0x11, 0x16));
        SetBorderBg(WallbangPill, _activeType == "wallbang" ? selBg : defBg);
        SetBorderBg(BoostPill, _activeType == "boost" ? selBg : defBg);
        SetBorderBg(JumpPill, _activeType == "jump" ? selBg : defBg);
        SetBorderBg(CampPill, _activeType == "camp" ? selBg : defBg);
        SetBorderBg(TrickProPill, _proOnly ? selBg : defBg);
        SetTextFg(TrickProPillText, _proOnly ? dk : gr);
        bool canFilterSide = _activeType is "wallbang" or "boost";
        SetBorderBg(TrickTPill, canFilterSide ? (_activeSide == "T" ? actBg : defBg) : defBg);
        SetBorderBg(TrickCTPill, canFilterSide ? (_activeSide == "CT" ? actBg : defBg) : defBg);
        SetBorderBg(TrickBothPill, canFilterSide ? (_activeSide == "Both" ? actBg : defBg) : defBg);
        SetTextFg(TrickFilterT, canFilterSide ? (_activeSide == "T" ? dk : gr) : new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)));
        SetTextFg(TrickFilterCT, canFilterSide ? (_activeSide == "CT" ? dk : gr) : new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)));
        SetTextFg(TrickFilterBoth, canFilterSide ? (_activeSide == "Both" ? dk : gr) : new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)));
        TrickTPill.IsHitTestVisible = canFilterSide;
        TrickCTPill.IsHitTestVisible = canFilterSide;
        TrickBothPill.IsHitTestVisible = canFilterSide;
        // Floor pills
        SetBorderBg(TricksUpperPill, _activeFloor == "default" ? actBg : defBg);
        SetBorderBg(TricksLowerPill, _activeFloor == "lower" ? actBg : defBg);
        SetTextFg(TricksFloorDefault, _activeFloor == "default" ? dk : gr);
        SetTextFg(TricksFloorLower, _activeFloor == "lower" ? dk : gr);
    }

    // PRO filter handlers
    private void ProFilter_Click(object sender, MouseButtonEventArgs e)
    {
        _proOnly = !_proOnly;
        DrawTargets();
        UpdateFilterHighlights();
    }
    private void TrickProFilter_Click(object sender, MouseButtonEventArgs e)
    {
        _proOnly = !_proOnly;
        if (_activeType is "wallbang" or "jump") ReloadTricks();
        else DrawTricks();
        UpdateTrickFilterHighlights();
    }

    private void ToggleSidePanel_Click(object sender, RoutedEventArgs e)
    {
        bool expand = SidePanel.Visibility == Visibility.Collapsed;
        SidePanel.Visibility = expand ? Visibility.Visible : Visibility.Collapsed;
        SideToggleBtn.Visibility = expand ? Visibility.Collapsed : Visibility.Visible;
    }
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_dataService == null) return;
        var query = SearchBox.Text.Trim().ToLower();
        TargetList.Items.Clear();

        // Search across ALL floors for the query
        var allTargets = _dataService.GetAllTargets(_activeProfile!.Id, _mapId, _activeType).Where(t => (_activeSide == "Both" || t.Side == _activeSide)).ToList();
        var filtered = string.IsNullOrEmpty(query)
            ? allTargets
            : allTargets.Where(t => t.Name.ToLower().Contains(query)).ToList();

        foreach (var t in filtered)
        {
            var item = new ListBoxItem
            {
                Content = t.Name + " (" + t.Floor + ")",
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 12,
                Tag = t.Id,
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1a, 0x1d, 0x24))
            };
            item.MouseDoubleClick += (s, args) =>
            {
                if (s is ListBoxItem lbi && lbi.Tag is Guid tid)
                {
                    _highlightedTargetId = tid;
                    DrawTargets();
                    // Center map on target
                    var target = _mapTargets.FirstOrDefault(x => x.Id == tid);
                    if (target != null)
                    {
                        MapTranslate.X = (ContainerW / 2) - target.X * _zoom;
                        MapTranslate.Y = (ContainerH / 2) - target.Y * _zoom;
                        ClampMap();
                        DrawHighlight(tid);
                    }
                }
            };
            TargetList.Items.Add(item);
        }
    }
}
















