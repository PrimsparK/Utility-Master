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
    private LineupSpot? HitTestLineupSpot(Point cpt)
    {
        return _lineupSpots.FirstOrDefault(s => Dist(cpt, s.X, s.Y) < 18);
    }

    private TargetEntity? HitTestLineupTarget(Point cpt)
    {
        return _visibleLineupTargets.FirstOrDefault(t => _drawnTargetIds.Contains(t.Id) && Dist(cpt, t.X, t.Y) < 25);
    }

    private List<LineupEntity> GetSelectedSpotLineupsForTarget(Guid targetId)
    {
        if (_selectedLineupSpot is not (double sx, double sy)) return new();
        var spot = _lineupSpots.FirstOrDefault(g => Math.Abs(g.X - sx) < 0.01 && Math.Abs(g.Y - sy) < 0.01);
        if (spot == null) return new();
        return spot.Entries.Where(e => e.Target.Id == targetId).Select(e => e.Lineup).OrderBy(l => l.Sequence).ToList();
    }

    private List<LineupEntity> GetVisibleLineupsForTarget(TargetEntity target)
    {
        return target.Lineups
            .Where(l => l.Floor == _activeFloor && (_activeSide == "Both" || (l.Side ?? "T") == _activeSide) && (!_proOnly || l.IsPro))
            .OrderBy(l => l.Sequence)
            .ToList();
    }

    private (TargetEntity? t, bool isL, LineupEntity? l, List<LineupEntity>? siblings) HitTestAll(Point cpt)
    {
        if (_highlightedTargetId.HasValue) { var ht = _mapTargets.FirstOrDefault(x => x.Id == _highlightedTargetId.Value); if (ht != null) foreach (var l in ht.Lineups.Where(x => x.Floor == _activeFloor && (_activeSide == "Both" || (x.Side ?? "T") == _activeSide) && (!_proOnly || x.IsPro)).OrderBy(x => x.Sequence)) if (Dist(cpt, l.X, l.Y) < 16) { var sibs = l.GroupId != null ? ht.Lineups.Where(x => x.GroupId == l.GroupId && x.Floor == _activeFloor && (_activeSide == "Both" || (x.Side ?? "T") == _activeSide) && (!_proOnly || x.IsPro)).OrderBy(x => x.Sequence).ToList() : null; return (ht, true, l, sibs); } }
        foreach (var t in _mapTargets.Where(x => x.Type == _activeType && (_activeSide == "Both" || x.Side == _activeSide) && x.Floor == _activeFloor && (!_proOnly || x.Lineups.Any(l => l.IsPro)))) if (Dist(cpt, t.X, t.Y) < 25) return (t, false, null, null);
        return (null, false, null, null);
    }

    private TrickEntity? HitTestTrick(Point cpt)
    {
        bool wallbangBoost = _activeType is "wallbang" or "boost";
        foreach (var t in _mapTricks.Where(x => x.Type == _activeType && x.Floor == _activeFloor && (!wallbangBoost || _activeSide == "Both" || x.Side == _activeSide))) if (Dist(cpt, t.X, t.Y) < 20) return t;
        return null;
    }

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

        if (_nadesBrowseMode == "lineups")
        {
            var target = HitTestLineupTarget(cpt);
            if (target != null)
            {
                var related = GetSelectedSpotLineupsForTarget(target.Id);
                if (related.Count == 0) related = GetVisibleLineupsForTarget(target);
                if (related.Count == 1)
                {
                    new LineupDetailWindow(related[0], target) { Owner = Window.GetWindow(this) }.ShowDialog();
                }
                else if (related.Count > 1)
                {
                    ShowLineupVariantPicker(related, target);
                }
                else
                {
                    _highlightedTargetId = target.Id;
                    _selectedLineupSpot = null;
                    DrawLineups();
                }
                return;
            }

            var spot = HitTestLineupSpot(cpt);
            if (spot != null)
            {
                _highlightedTargetId = null;
                var key = (spot.X, spot.Y);
                _selectedLineupSpot = _selectedLineupSpot is (double sx, double sy) &&
                    Math.Abs(sx - spot.X) < 0.01 && Math.Abs(sy - spot.Y) < 0.01
                    ? null
                    : key;
                DrawLineups();
            }
            else if (_selectedLineupSpot.HasValue)
            {
                _highlightedTargetId = null;
                _selectedLineupSpot = null;
                DrawLineups();
            }
            else if (_highlightedTargetId.HasValue)
            {
                _highlightedTargetId = null;
                DrawLineups();
            }
            return;
        }

        if (_nadesBrowseMode == "lineups")
        {
            var lineupTarget = HitTestLineupTarget(cpt) ?? _visibleLineupTargets.FirstOrDefault(x => Dist(cpt, x.X, x.Y) < 25);
            if (lineupTarget != null)
            {
                ShowPreviewMarker(lineupTarget.X, lineupTarget.Y);
                var cm = new ContextMenu();
                var tid = lineupTarget.Id;
                cm.Items.Add(Mi(Loc.Get("map.edit_target") + " " + lineupTarget.Name, () => EditTargetT(lineupTarget)));
                cm.Items.Add(Mi(Loc.F("map.delete_target", lineupTarget.Name), () => DeleteTarget(tid)));
                cm.Items.Add(Mi(Loc.Get("map.add_lineup"), () => AddLineupToTarget(tid)));
                cm.Items.Add(new Separator());

                var related = GetSelectedSpotLineupsForTarget(lineupTarget.Id);
                if (related.Count == 0) related = GetVisibleLineupsForTarget(lineupTarget);
                foreach (var lineup in related)
                {
                    var selLineup = lineup;
                    cm.Items.Add(Mi(Loc.Get("edit") + " #" + selLineup.Sequence, () => EditLineupT(lineupTarget, selLineup)));
                    cm.Items.Add(Mi(Loc.Get("delete") + " #" + selLineup.Sequence, () => DeleteLineupT(lineupTarget, selLineup)));
                }
                cm.PlacementTarget = sender as UIElement;
                cm.Closed += (_, _) => RemovePreviewMarker();
                cm.IsOpen = true;
                return;
            }

            var spot = HitTestLineupSpot(cpt);
            if (spot != null)
            {
                ShowPreviewMarker(spot.X, spot.Y);
                var cm = new ContextMenu();
                foreach (var entry in spot.Entries)
                {
                    var spotTarget = entry.Target;
                    var lineup = entry.Lineup;
                    var label = "#" + lineup.Sequence + " " + MapRenderingHelpers.GetNadeTypeLabel(spotTarget.Type, lineup.Side);
                    cm.Items.Add(Mi(Loc.Get("edit") + " " + label, () => EditLineupT(spotTarget, lineup)));
                    cm.Items.Add(Mi(Loc.Get("delete") + " " + label, () => DeleteLineupT(spotTarget, lineup)));
                }
                cm.Items.Add(new Separator());

                foreach (var group in spot.Entries.GroupBy(e => e.Target.Id))
                {
                    var spotTarget = group.First().Target;
                    var spotTargetId = spotTarget.Id;
                    cm.Items.Add(Mi(Loc.Get("map.edit_target") + " " + spotTarget.Name, () => EditTargetT(spotTarget)));
                    cm.Items.Add(Mi(Loc.F("map.delete_target", spotTarget.Name), () => DeleteTarget(spotTargetId)));
                }
                cm.Items.Add(Mi(Loc.Get("map.add_target"), () => OpenCreateTargetAt(cpt.X, cpt.Y)));
                cm.PlacementTarget = sender as UIElement;
                cm.Closed += (_, _) => RemovePreviewMarker();
                cm.IsOpen = true;
                return;
            }

            ShowPreviewMarker(cpt.X, cpt.Y);
            var emptyMenu = new ContextMenu();
            emptyMenu.Items.Add(Mi(Loc.Get("map.add_target"), () => OpenCreateTargetAt(cpt.X, cpt.Y)));
            emptyMenu.PlacementTarget = sender as UIElement;
            emptyMenu.Closed += (_, _) => RemovePreviewMarker();
            emptyMenu.IsOpen = true;
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

    private void ToggleSidePanel_Click(object sender, RoutedEventArgs e)
    {
        bool expand = SidePanel.Visibility == Visibility.Collapsed;
        SidePanel.Visibility = expand ? Visibility.Visible : Visibility.Collapsed;
        SideToggleBtn.Visibility = expand ? Visibility.Collapsed : Visibility.Visible;
        if (expand)
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
        }
    }
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_dataService == null) return;
        var query = SearchBox.Text.Trim().ToLower();
        TargetList.Items.Clear();
        ClearSearchBtn.Visibility = string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Collapsed : Visibility.Visible;

        var typeFilter = _nadesBrowseMode == "lineups" ? _lineupTypes : new HashSet<string> { _activeType };
        var allTargets = _dataService.GetAllTargets(_activeProfile!.Id, _mapId)
            .Where(t => typeFilter.Contains(t.Type) && (_activeSide == "Both" || t.Side == _activeSide) && (!_proOnly || t.Lineups.Any(l => l.IsPro)))
            .ToList();
        var filtered = string.IsNullOrEmpty(query)
            ? allTargets
            : allTargets.Where(t => t.Name.ToLower().Contains(query)).ToList();
        var total = filtered.Count;

        foreach (var t in filtered)
        {
            var meta = GetSpotTypeLabel(t.Type, t.Side) + " · " + (t.Side ?? "?") + " · " + t.Floor;
            TargetList.Items.Add(CreateSearchItem(t.Name, meta, t.Id, () => LocateTarget(t.Id)));
        }

        if (_mode == "tricks" && _activeType is "boost" or "camp")
        {
            var tricks = _dataService.GetTricks(_activeProfile!.Id, _mapId)
                .Where(t => t.Type == _activeType && t.Floor == _activeFloor &&
                    (_activeType != "boost" || _activeSide == "Both" || t.Side == _activeSide))
                .ToList();
            var filteredTricks = string.IsNullOrEmpty(query)
                ? tricks
                : tricks.Where(t => t.Name.ToLower().Contains(query)).ToList();
            total += filteredTricks.Count;

            foreach (var trick in filteredTricks)
            {
                var side = trick.Side == null ? "" : " · " + trick.Side;
                var meta = GetSpotTypeLabel(trick.Type, trick.Side) + side + " · " + trick.Floor;
                TargetList.Items.Add(CreateSearchItem(trick.Name, meta, "tr:" + trick.Id, () => OpenTrickFromSearch(trick.Id)));
            }
        }

        SearchResultCount.Text = Loc.F("map.search_count", total);
        SearchResultCount.Visibility = total == 0 ? Visibility.Collapsed : Visibility.Visible;
        SearchEmptyText.Visibility = total == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ClearSearch_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = "";
        SearchBox.Focus();
    }

    private void LocateTarget(Guid tid)
    {
        if (_nadesBrowseMode == "lineups")
        {
            _nadesBrowseMode = "targets";
            UpdateFilterHighlights();
        }
        _highlightedTargetId = tid;
        var target = _mapTargets.FirstOrDefault(x => x.Id == tid);
        if (target != null && target.Floor != _activeFloor)
        {
            _activeFloor = target.Floor;
            SwitchFloorRadar();
            UpdateFilterHighlights();
        }
        DrawTargets();
        if (target != null)
        {
            MapTranslate.X = (ContainerW / 2) - target.X * _zoom;
            MapTranslate.Y = (ContainerH / 2) - target.Y * _zoom;
            ClampMap();
            DrawHighlight(tid);
        }
    }

    private void OpenTrickFromSearch(Guid trickId)
    {
        var trick = _mapTricks.FirstOrDefault(x => x.Id == trickId);
        if (trick != null) ShowTrickDetail(trick);
    }

    private ListBoxItem CreateSearchItem(string title, string meta, object tag, Action open)
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition());

        var name = new TextBlock
        {
            Text = title,
            Foreground = Brushes.White,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        };
        var metaText = new TextBlock
        {
            Text = meta,
            Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
            FontSize = 10,
            Margin = new Thickness(0, 3, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Grid.SetRow(name, 0);
        Grid.SetRow(metaText, 1);
        grid.Children.Add(name);
        grid.Children.Add(metaText);

        var item = new ListBoxItem
        {
            Content = grid,
            Tag = tag,
            Cursor = Cursors.Hand
        };
        item.MouseDoubleClick += (s, args) =>
        {
            if (s is ListBoxItem lbi && Equals(lbi.Tag, tag)) open();
        };
        return item;
    }

    private static string GetSpotTypeLabel(string type, string? side)
    {
        return type is "smoke" or "flash" or "he" or "molotov"
            ? MapRenderingHelpers.GetNadeTypeLabel(type, side)
            : Loc.Get(type);
    }
}
