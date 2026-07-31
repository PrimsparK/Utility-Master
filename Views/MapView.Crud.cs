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
    public void OpenCreateTargetAt(double x, double y)
    {
        var creationType = _nadesBrowseMode == "lineups" && _lineupTypes.Count > 0
            ? _lineupTypes.OrderBy(MapRenderingHelpers.GetNadeTypeOrder).First()
            : _activeType;

        // Check if there's a nearby existing target of the same type/side/floor to merge into
        var settings = SettingsService.Load();
        double threshold = settings.TargetConflictRadius;
        var nearbyTarget = _mapTargets.FirstOrDefault(t =>
            t.Type == creationType && t.Floor == _activeFloor &&
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
        w.PreSelectType(creationType);
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
        _highlightedTargetId = null;
        ReloadTargets();
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
        if (t.Lineups.Count <= 1)
        {
            var deleteTarget = MessageBox.Show(
                Loc.F("map.delete_last_lineup_confirm", t.Name),
                Loc.Get("map.delete_title"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (deleteTarget == MessageBoxResult.Yes) DeleteTarget(t.Id);
            return;
        }
        if (l.IsDefault && _activeProfile != null && !_activeProfile.AllowDeleteDefaultSpots) { MessageBox.Show(Loc.Get("map.protected"), Loc.Get("map.protected_title"), MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (MessageBox.Show(Loc.F("map.delete_lineup_confirm", l.Sequence), Loc.Get("map.delete_lineup_title"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        var fresh = _dataService.GetLineup(l.Id);
        if (fresh == null) return;
        _dataService.DeleteLineup(fresh.Id);
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
                        Loc.F("map.overwrite_lineup", nearby.Sequence),
                        Loc.Get("map.lineup_variant_title"), MessageBoxButton.YesNo, MessageBoxImage.Question);
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
                        Loc.F("map.new_variant", nearbyLineup.Sequence),
                        Loc.Get("map.lineup_variant_title"), MessageBoxButton.YesNo, MessageBoxImage.Question);
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
}
