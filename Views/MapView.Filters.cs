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
    private static void SetBorderBg(Border border, Brush bg) => border.Background = bg;
    private static void SetTextFg(TextBlock tb, Brush fg) => tb.Foreground = fg;

    private void Filter_Click(object sender, MouseButtonEventArgs e)
    {
        var tag = ((FrameworkElement)sender).Tag?.ToString();
        if (tag == null) return;
        if (_nadesBrowseMode == "lineups")
        {
            if (_lineupTypes.Contains(tag))
            {
                if (_lineupTypes.Count > 1) _lineupTypes.Remove(tag);
            }
            else
            {
                _lineupTypes.Add(tag);
            }
            DrawLineups();
        }
        else
        {
            _activeType = tag;
            DrawTargets();
        }
        UpdateFilterHighlights();
    }

    private void NadesBrowseMode_Click(object sender, MouseButtonEventArgs e)
    {
        var tag = ((FrameworkElement)sender).Tag?.ToString();
        if (tag is "targets" or "lineups")
        {
            _nadesBrowseMode = tag;
            _selectedLineupSpot = null;
            _highlightedTargetId = null;
            DrawNades();
            UpdateFilterHighlights();
        }
    }

    private void Side_Click(object sender, MouseButtonEventArgs e) { var tag = ((FrameworkElement)sender).Tag?.ToString(); if (tag != null) { _activeSide = tag; DrawNades(); UpdateFilterHighlights(); } }
    private void Floor_Click(object sender, MouseButtonEventArgs e) { var tag = ((FrameworkElement)sender).Tag?.ToString(); if (tag != null) { _activeFloor = tag; SwitchFloorRadar(); if (_mode == "nades") { DrawNades(); UpdateFilterHighlights(); } else { ReloadTricks(); UpdateTrickFilterHighlights(); } } }

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
        bool lineupMode = _nadesBrowseMode == "lineups";
        bool smokeActive = lineupMode ? _lineupTypes.Contains("smoke") : _activeType == "smoke";
        bool flashActive = lineupMode ? _lineupTypes.Contains("flash") : _activeType == "flash";
        bool mollyActive = lineupMode ? _lineupTypes.Contains("molotov") : _activeType == "molotov";
        bool heActive = lineupMode ? _lineupTypes.Contains("he") : _activeType == "he";
        SetBorderBg(SmokePill, smokeActive ? selBg : defBg);
        SetBorderBg(FlashPill, flashActive ? selBg : defBg);
        SetBorderBg(MollyPill, mollyActive ? selBg : defBg);
        SetBorderBg(HEPill, heActive ? selBg : defBg);
        SetBorderBg(TargetBrowsePill, lineupMode ? defBg : actBg);
        SetBorderBg(LineupBrowsePill, lineupMode ? actBg : defBg);
        SetTextFg(TargetBrowseText, lineupMode ? gr : dk);
        SetTextFg(LineupBrowseText, lineupMode ? dk : gr);
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
        DrawNades();
        UpdateFilterHighlights();
    }
    private void TrickProFilter_Click(object sender, MouseButtonEventArgs e)
    {
        _proOnly = !_proOnly;
        if (_activeType is "wallbang" or "jump") ReloadTricks();
        else DrawTricks();
        UpdateTrickFilterHighlights();
    }
}
