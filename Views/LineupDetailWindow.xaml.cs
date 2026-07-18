using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UtilityMaster.Models;
using UtilityMaster.Services;

namespace UtilityMaster.Views;

public partial class LineupDetailWindow : Window
{
    private readonly List<string> _imagePaths = new();
    private string? _videoUrl;

    private List<LineupEntity> _siblings = new();
    private int _currentSiblingIndex = -1;

    public LineupDetailWindow(LineupEntity lineup, TargetEntity? target = null, List<LineupEntity>? siblings = null)
    {
        InitializeComponent();
        _siblings = siblings ?? new List<LineupEntity>();
        if (_siblings.Count <= 1) _siblings = new List<LineupEntity>();

        var lineupSide = lineup.Side ?? target?.Side ?? "T";
        var type = target?.Type ?? "?";
        var lineupName = !string.IsNullOrWhiteSpace(lineup.Name) ? lineup.Name : (target?.Name ?? "?");

        var typeText = type switch
        {
            "smoke" => Loc.Get("smoke"),
            "flash" => Loc.Get("flash"),
            "he" => Loc.Get("he"),
            "molotov" => lineupSide == "CT" ? "Incendiary" : "Molotov",
            _ => type
        };

        Title = Loc.Get("lineup.title") + " - #" + lineup.Sequence;
        TitleLabel.Text = Loc.Get("lineup.title") + " #" + lineup.Sequence;
        SideInfoText.Text = lineupSide + " " + typeText;
        TargetTitle.Text = Loc.Get("lineup.target_label").Replace("{0}", lineupName);
        TargetNameText.Text = lineupName;
        CoordText.Text = "(" + lineup.X.ToString("F0") + ", " + lineup.Y.ToString("F0") + ")";

        AimDescTitle.Text = Loc.Get("lineup.aim");
        AimDescText.Text = lineup.AimDescription ?? Loc.Get("lineup.none");

        ThrowTypeTitle.Text = Loc.Get("lineup.throw_type");
        ThrowTypeText.Text = (lineup.ThrowType ?? "standing") switch
        {
            "standing" => Loc.Get("standing"),
            "crouching" => Loc.Get("crouching"),
            "jump-throw" => Loc.Get("jump_throw"),
            "running" => Loc.Get("running"),
            _ => lineup.ThrowType ?? "standing"
        };

        NotesTitle.Text = Loc.Get("lineup.notes");
        NotesText.Text = lineup.Notes ?? Loc.Get("lineup.none");

        LoadImages(lineup.ImagesJson);

        // Variant navigation (sibling lineups)
        if (_siblings.Count > 1)
        {
            VariantNav.Visibility = Visibility.Visible;
            _currentSiblingIndex = _siblings.IndexOf(lineup);
            if (_currentSiblingIndex < 0) _currentSiblingIndex = 0;
            VariantPrevBtn.Click += (s, e) => NavigateVariant(-1);
            VariantNextBtn.Click += (s, e) => NavigateVariant(1);
            UpdateVariantLabel();
        }

        // Aim Points section

        // Handle video
        if (!string.IsNullOrWhiteSpace(lineup.VideoUrl))
        {
            _videoUrl = lineup.VideoUrl;
            LoadVideo(lineup.VideoUrl);
        }
    }

    private async void LoadVideo(string url)
    {
        VideoSection.Visibility = Visibility.Visible;
        VideoTitle.Text = Loc.Get("lineup.video");

        if (File.Exists(url))
        {
            // Local file: use MediaElement
            try
            {
                VideoPlayer.Source = new Uri(url);
                VideoPlayer.Visibility = Visibility.Visible;
                VideoControls.Visibility = Visibility.Visible;
                var sets = SettingsService.Load();
                if (sets.AutoPlayVideo) VideoPlayer.Play();
            }
            catch { /* media failed */ }
        }
        else
        {
            WebViewContainer.Visibility = Visibility.Visible;
            await VideoWebView.EnsureCoreWebView2Async();
            VideoWebView.CoreWebView2.Settings.IsWebMessageEnabled = false;
            VideoWebView.CoreWebView2.NewWindowRequested += (s, args) =>
            {
                args.Handled = true; // Block all popup windows
            };
            VideoWebView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = false;
            VideoWebView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
            VideoWebView.CoreWebView2.NavigationStarting += (s, args) =>
            {
                // Only allow navigation to youtube.com/embed or bilibili player
                var uri = args.Uri?.ToLower() ?? "";
                if (!string.IsNullOrEmpty(uri) && 
                    !uri.Contains("youtube.com/embed") && 
                    !uri.Contains("player.bilibili.com") &&
                    !uri.StartsWith("data:") &&
                    !uri.StartsWith("about:"))
                {
                    args.Cancel = true; // Block external navigation
                }
            };

            string embedHtml;
            if (url.Contains("youtube.com/watch") || url.Contains("youtu.be/"))
            {
                var vid = ExtractYouTubeId(url);
                embedHtml = $@"<!DOCTYPE html><html style='margin:0;padding:0;background:#000;overflow:hidden;width:100%;height:100%'>
<body style='margin:0;padding:0;background:#000;overflow:hidden'>
<iframe width='100%' height='100%' src='https://www.youtube.com/embed/{vid}?autoplay=1&rel=0&controls=1&modestbranding=1&playsinline=1'
    frameborder='0' allow='autoplay;encrypted-media' allowfullscreen style='position:fixed;top:0;left:0;width:100%;height:100%;border:none'></iframe>
</body></html>";
            }
            else if (url.Contains("bilibili.com/video/"))
            {
                var bvid = ExtractBilibiliId(url);
                embedHtml = $@"<!DOCTYPE html><html style='margin:0;padding:0;background:#000;overflow:hidden;width:100%;height:100%'>
<body style='margin:0;padding:0;background:#000;overflow:hidden'>
<iframe width='100%' height='100%' src='https://player.bilibili.com/player.html?bvid={bvid}&autoplay=1&danmaku=0&high_quality=1'
    frameborder='0' allow='autoplay' allowfullscreen style='position:fixed;top:0;left:0;width:100%;height:100%;border:none'></iframe>
</body></html>";
            }
            else
            {
                // Other URLs: embed full page but with no extra UI
                embedHtml = $@"<!DOCTYPE html><html style='margin:0;padding:0;background:#000;overflow:hidden;width:100%;height:100%'>
<body style='margin:0;padding:0;background:#000;overflow:hidden'>
<iframe width='100%' height='100%' src='{url}' frameborder='0' allow='autoplay;encrypted-media' allowfullscreen
    style='position:fixed;top:0;left:0;width:100%;height:100%;border:none'></iframe>
</body></html>";
            }
            VideoWebView.NavigateToString(embedHtml);
        }
    }



    private static string ExtractYouTubeId(string url)
    {
        if (url.Contains("youtu.be/"))
        {
            var part = url.Split("youtu.be/")[1].Split('?')[0].Split('&')[0].Split('#')[0];
            return part;
        }
        try
        {
            var uri = new Uri(url);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            return query["v"] ?? "";
        }
        catch { return ""; }
    }

    private static string ExtractBilibiliId(string url)
    {
        try
        {
            var parts = url.Split("bilibili.com/video/");
            if (parts.Length > 1)
                return parts[1].Split('?')[0].Split('/')[0].Split('#')[0].TrimEnd('/');
        }
        catch { }
        return "";
    }

    private void NavigateVariant(int delta)
    {
        if (_siblings.Count == 0) return;
        _currentSiblingIndex = (_currentSiblingIndex + delta + _siblings.Count) % _siblings.Count;
        var sel = _siblings[_currentSiblingIndex];
        // Reload the window content with the selected lineup
        RefreshContent(sel);
        UpdateVariantLabel();
    }

    private void UpdateVariantLabel()
    {
        VariantLabel.Text = (_currentSiblingIndex + 1) + "/" + _siblings.Count;
    }

    private void RefreshContent(LineupEntity lineup)
    {
        Title = Loc.Get("lineup.title") + " - #" + lineup.Sequence;
        TitleLabel.Text = Loc.Get("lineup.title") + " #" + lineup.Sequence;
        CoordText.Text = "(" + lineup.X.ToString("F0") + ", " + lineup.Y.ToString("F0") + ")";
        AimDescText.Text = lineup.AimDescription ?? Loc.Get("lineup.none");
        ThrowTypeText.Text = (lineup.ThrowType ?? "standing") switch
        {
            "standing" => Loc.Get("standing"),
            "crouching" => Loc.Get("crouching"),
            "jump-throw" => Loc.Get("jump_throw"),
            "running" => Loc.Get("running"),
            _ => lineup.ThrowType ?? "standing"
        };
        NotesText.Text = lineup.Notes ?? Loc.Get("lineup.none");
        LoadImages(lineup.ImagesJson);
        if (!string.IsNullOrWhiteSpace(lineup.VideoUrl))
        {
            _videoUrl = lineup.VideoUrl;
            LoadVideo(lineup.VideoUrl);
        }
        else
        {
            VideoSection.Visibility = Visibility.Collapsed;
        }
    }




    private void LoadImages(string imagesJson)
    {
        try
        {
            var paths = JsonSerializer.Deserialize<List<string>>(imagesJson ?? "[]");
            if (paths == null || paths.Count == 0) { ImagesSection.Visibility = Visibility.Collapsed; return; }

            _imagePaths.Clear();
            _imagePaths.AddRange(paths.Where(File.Exists));
            if (_imagePaths.Count == 0) { ImagesSection.Visibility = Visibility.Collapsed; return; }

            ImagesSection.Visibility = Visibility.Visible;
            ShowPrimaryImage(_imagePaths[0]);

            ThumbnailPanel.Children.Clear();
            for (int i = 0; i < _imagePaths.Count; i++)
            {
                var idx = i;
                var p = _imagePaths[i];
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(p);
                bmp.EndInit();
                bmp.Freeze();

                var thumb = new Border
                {
                    Width = 80, Height = 60,
                    Margin = new Thickness(0, 0, 4, 0),
                    Background = new SolidColorBrush(Color.FromRgb(0x0f, 0x11, 0x16)),
                    BorderBrush = idx == 0
                        ? new SolidColorBrush(Color.FromRgb(0xf5, 0xa6, 0x23))
                        : new SolidColorBrush(Color.FromRgb(0x2a, 0x2d, 0x34)),
                    BorderThickness = new Thickness(idx == 0 ? 2 : 1),
                    Cursor = Cursors.Hand,
                    Child = new Image { Source = bmp, Stretch = Stretch.Uniform }
                };
                thumb.MouseLeftButtonDown += (s, e) =>
                {
                    if (e.ClickCount == 2)
                        ShowFullImage(p);
                    else
                    {
                        ShowPrimaryImage(p);
                        for (int j = 0; j < ThumbnailPanel.Children.Count; j++)
                        {
                            if (ThumbnailPanel.Children[j] is Border b)
                                b.BorderBrush = j == idx
                                    ? new SolidColorBrush(Color.FromRgb(0xf5, 0xa6, 0x23))
                                    : new SolidColorBrush(Color.FromRgb(0x2a, 0x2d, 0x34));
                        }
                    }
                };
                ThumbnailPanel.Children.Add(thumb);
            }
            ImageCountText.Text = _imagePaths.Count + " image(s)";
        }
        catch { }
    }

    private void ShowPrimaryImage(string path)
    {
        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.UriSource = new Uri(path);
        bmp.EndInit();
        bmp.Freeze();
        PrimaryImage.Source = bmp;
        PrimaryImage.Cursor = Cursors.Hand;
        PrimaryImage.MouseLeftButtonDown -= OnPrimaryClick;
        PrimaryImage.MouseLeftButtonDown += OnPrimaryClick;
    }

    private void OnPrimaryClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && PrimaryImage.Source is BitmapImage bmp && bmp.UriSource != null)
        {
            var lp = bmp.UriSource.LocalPath;
            if (!string.IsNullOrEmpty(lp)) ShowFullImage(lp);
        }
    }

    private void ShowFullImage(string path)
    {
        var win = new Window
        {
            Title = "Image Preview",
            Width = SystemParameters.PrimaryScreenWidth * 0.9,
            Height = SystemParameters.PrimaryScreenHeight * 0.9,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Background = new SolidColorBrush(Color.FromRgb(0x0a, 0x0b, 0x0e)),
            Owner = this
        };
        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.UriSource = new Uri(path);
        bmp.EndInit();
        bmp.Freeze();
        win.Content = new Image { Source = bmp, Stretch = Stretch.Uniform };
        win.ShowDialog();
    }

    private void VideoPlay_Click(object sender, RoutedEventArgs e) => VideoPlayer.Play();
    private void VideoPause_Click(object sender, RoutedEventArgs e) => VideoPlayer.Pause();
    private void VideoStop_Click(object sender, RoutedEventArgs e) => VideoPlayer.Stop();

}
