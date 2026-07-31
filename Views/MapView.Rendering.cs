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

public partial class MapView
{
    private void DrawTargets()
    {
        var visible = _mapTargets.Where(t => t.Type == _activeType && (_activeSide == "Both" || t.Side == _activeSide) && t.Floor == _activeFloor && (!_proOnly || t.Lineups.Any(l => l.IsPro))).ToList();
        
        // Remove old nades overlays
        // Clear border highlights from target icons
        foreach (var b in MapCanvas.Children.OfType<Border>().Where(b => { var t = b.Tag?.ToString() ?? ""; return t.StartsWith("t:"); }))
            b.BorderBrush = Brushes.Transparent;
        foreach (var c in MapCanvas.Children.OfType<FrameworkElement>().Where(c => { var t = c.Tag?.ToString() ?? ""; return t.StartsWith("t:") || t.StartsWith("l:") || t.StartsWith("ls") || t == "dash" || t == "glow"; }).ToList())
            MapCanvas.Children.Remove(c);
        RemovePreviewMarker();
        
        foreach (var t in visible) { var i = CreateTargetIcon(t); Canvas.SetLeft(i, t.X - 16); Canvas.SetTop(i, t.Y - 16); MapCanvas.Children.Add(i); }
        if (_highlightedTargetId.HasValue && visible.Any(t => t.Id == _highlightedTargetId.Value)) DrawHighlight(_highlightedTargetId.Value);
        else _highlightedTargetId = null;
    }

    private void DrawLineups()
    {
        ClearMapOverlays();
        _lineupSpots.Clear();
        _visibleLineupTargets.Clear();
        _drawnTargetIds.Clear();

        var result = LineupSpotBuilder.Build(_mapTargets, _lineupTypes, _activeSide, _activeFloor, _proOnly);
        _visibleLineupTargets = result.VisibleTargets;
        var groups = result.Spots;
        _lineupSpots = groups;

        if (_selectedLineupSpot is (double sx, double sy) &&
            !groups.Any(g => Math.Abs(g.X - sx) < 0.01 && Math.Abs(g.Y - sy) < 0.01))
        {
            _selectedLineupSpot = null;
        }

        if (_highlightedTargetId is Guid focusId)
        {
            var target = _visibleLineupTargets.FirstOrDefault(t => t.Id == focusId);
            if (target == null)
            {
                _highlightedTargetId = null;
            }
            else
            {
                _selectedLineupSpot = null;
                _drawnTargetIds.Add(target.Id);
                DrawTargetFocusInLineupMode(target);
                return;
            }
        }

        foreach (var spot in groups)
        {
            bool isSelected = _selectedLineupSpot is (double selX, double selY) &&
                Math.Abs(selX - spot.X) < 0.01 && Math.Abs(selY - spot.Y) < 0.01;
            var icon = CreateLineupSpotIcon(spot, isSelected);
            Canvas.SetLeft(icon, spot.X - 16);
            Canvas.SetTop(icon, spot.Y - 16);
            MapCanvas.Children.Add(icon);
        }

        if (_selectedLineupSpot is (double px, double py))
        {
            var spot = groups.FirstOrDefault(g => Math.Abs(g.X - px) < 0.01 && Math.Abs(g.Y - py) < 0.01);
            if (spot != null)
            {
                foreach (var entry in spot.Entries) _drawnTargetIds.Add(entry.Target.Id);
                DrawLineupSpotHighlight(spot);
            }
        }
    }


    private Border CreateLineupSpotIcon(LineupSpot spot, bool isSelected = false)
    {
        var types = spot.Entries.Select(e => e.Target.Type).Distinct().OrderBy(MapRenderingHelpers.GetNadeTypeOrder).ToList();
        var sides = spot.Entries.Select(e => e.Lineup.Side ?? "T").Distinct().ToList();
        var sideBrush = sides.Count == 1
            ? GetLineupSideBrush(sides[0])
            : new SolidColorBrush(Color.FromRgb(0x8f, 0x9a, 0xa8));

        var g = new Grid();
        g.Children.Add(new Border
        {
            Width = 30,
            Height = 30,
            CornerRadius = new CornerRadius(7),
            Background = new SolidColorBrush(Color.FromRgb(0x1a, 0x1d, 0x24)),
            BorderBrush = sideBrush,
            BorderThickness = new Thickness(2)
        });

        if (types.Count == 1)
        {
            var img = CreateUtilityImage(types[0], 17);
            if (img != null) g.Children.Add(img);
        }
        else
        {
            var mini = new Grid { Width = 22, Height = 22 };
            mini.RowDefinitions.Add(new RowDefinition());
            mini.RowDefinitions.Add(new RowDefinition());
            mini.ColumnDefinitions.Add(new ColumnDefinition());
            mini.ColumnDefinitions.Add(new ColumnDefinition());
            int index = 0;
            foreach (var type in types.Take(4))
            {
                var img = CreateUtilityImage(type, 10);
                if (img == null) continue;
                Grid.SetRow(img, index / 2);
                Grid.SetColumn(img, index % 2);
                mini.Children.Add(img);
                index++;
            }
            g.Children.Add(mini);
        }

        if (spot.Entries.Count > 1)
        {
            g.Children.Add(new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0xaa, 0xaa, 0xaa)),
                BorderThickness = new Thickness(0.6),
                CornerRadius = new CornerRadius(4),
                Width = 12,
                Height = 12,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 1, 1),
                Child = new TextBlock
                {
                    Text = spot.Entries.Count.ToString(),
                    FontSize = 7,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });
        }

        var tip = string.Join(", ", types.Select(t => MapRenderingHelpers.GetNadeTypeLabel(t, spot.Entries.First(e => e.Target.Type == t).Lineup.Side)));
        var marker = new Border
        {
            Child = g,
            Tag = "ls",
            Background = Brushes.Transparent,
            BorderBrush = isSelected ? new SolidColorBrush(Color.FromRgb(0xf5, 0xa6, 0x23)) : Brushes.Transparent,
            BorderThickness = isSelected ? new Thickness(2) : new Thickness(0),
            Width = 32,
            Height = 32,
            IsHitTestVisible = true,
            ToolTip = tip
        };
        Panel.SetZIndex(marker, isSelected ? 11 : 9);
        return marker;
    }

    private Image? CreateUtilityImage(string type, double size)
    {
        var fp = System.IO.Path.Combine(_iconsPath, MapRenderingHelpers.GetTargetIconFile(type, "T"));
        if (!File.Exists(fp)) return null;
        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.UriSource = new Uri(fp);
        bmp.EndInit();
        bmp.Freeze();
        return new Image { Source = bmp, Width = size, Height = size, Stretch = Stretch.Uniform };
    }

    private void DrawLineupSpotHighlight(LineupSpot spot)
    {
        foreach (var group in spot.Entries.GroupBy(e => e.Target.Id))
        {
            var target = group.First().Target;
            var color = MapRenderingHelpers.GetNadeTypeColor(target.Type);
            double dx = target.X - spot.X, dy = target.Y - spot.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < 0.01) dist = 1;
            double nx = dx / dist, ny = dy / dist;

            MapCanvas.Children.Add(new Line
            {
                X1 = spot.X + nx * 16,
                Y1 = spot.Y + ny * 16,
                X2 = target.X - nx * 16,
                Y2 = target.Y - ny * 16,
                Stroke = new SolidColorBrush(color) { Opacity = 0.8 },
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 3 },
                Tag = "dash"
            });

            var ti = CreateTargetIcon(target);
            Canvas.SetLeft(ti, target.X - 16);
            Canvas.SetTop(ti, target.Y - 16);
            MapCanvas.Children.Add(ti);
        }
    }

    private void DrawTargetFocusInLineupMode(TargetEntity target)
    {
        var targetIcon = CreateTargetIcon(target);
        targetIcon.BorderBrush = new SolidColorBrush(Color.FromRgb(0xf5, 0xa6, 0x23));
        targetIcon.BorderThickness = new Thickness(2);
        Canvas.SetLeft(targetIcon, target.X - 16);
        Canvas.SetTop(targetIcon, target.Y - 16);
        MapCanvas.Children.Add(targetIcon);

        var color = MapRenderingHelpers.GetNadeTypeColor(target.Type);
        var lineups = target.Lineups
            .Where(l => l.Floor == _activeFloor && (_activeSide == "Both" || (l.Side ?? "T") == _activeSide) && (!_proOnly || l.IsPro))
            .OrderBy(l => l.Sequence)
            .ToList();

        foreach (var lineup in lineups)
        {
            double dx = lineup.X - target.X, dy = lineup.Y - target.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < 0.01) dist = 1;
            double nx = dx / dist, ny = dy / dist;

            MapCanvas.Children.Add(new Line
            {
                X1 = target.X + nx * 16,
                Y1 = target.Y + ny * 16,
                X2 = lineup.X - nx * 12,
                Y2 = lineup.Y - ny * 12,
                Stroke = new SolidColorBrush(color) { Opacity = 0.8 },
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 3 },
                Tag = "dash"
            });

            var icon = CreateLineupIcon(lineup);
            Canvas.SetLeft(icon, lineup.X - 12);
            Canvas.SetTop(icon, lineup.Y - 12);
            Panel.SetZIndex(icon, 10);
            MapCanvas.Children.Add(icon);
        }
    }

    private static Brush GetLineupSideBrush(string side)
    {
        return side switch
        {
            "CT" => new SolidColorBrush(Color.FromRgb(0x3b, 0x7c, 0xc7)),
            "Both" => new SolidColorBrush(Color.FromRgb(0x8f, 0x9a, 0xa8)),
            _ => new SolidColorBrush(Color.FromRgb(0xde, 0x7b, 0x2c))
        };
    }

    private Border CreateTargetIcon(TargetEntity target)
    {
        var g = new Grid();
        g.Children.Add(new Ellipse { Width = 28, Height = 28, Fill = new SolidColorBrush(Color.FromRgb(0x1a, 0x1d, 0x24)), Stroke = new SolidColorBrush(Color.FromRgb(0x3a, 0x3d, 0x44)), StrokeThickness = 2 });
        var img = new Image { Width = 18, Height = 18, Stretch = Stretch.Uniform };
        var fp = System.IO.Path.Combine(_iconsPath, MapRenderingHelpers.GetTargetIconFile(target.Type, target.Side));
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

    private void DrawHighlight(Guid targetId)
    {
        foreach (var c in MapCanvas.Children.OfType<FrameworkElement>().Where(c => c.Tag?.ToString() is string t && (t.StartsWith("l:") || t == "dash" || t == "glow")).ToList()) MapCanvas.Children.Remove(c);
        var target = _mapTargets.FirstOrDefault(t => t.Id == targetId); if (target == null) return;
        // Add orange border highlight to the target icon
        var targetIcon = MapCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag?.ToString() == "t:" + targetId);
        if (targetIcon != null) targetIcon.BorderBrush = new SolidColorBrush(Color.FromRgb(0xf5, 0xa6, 0x23));
        
        var showLineups = target.Lineups
            .Where(l => l.Floor == _activeFloor && (_activeSide == "Both" || (l.Side ?? "T") == _activeSide) && (!_proOnly || l.IsPro))
            .OrderBy(l => l.Sequence)
            .ToList();

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
            var visible = _mapTricks.Where(t => t.Type == _activeType && t.Floor == _activeFloor && (!filterSide || _activeSide == "Both" || t.Side == _activeSide)).ToList();
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


}
