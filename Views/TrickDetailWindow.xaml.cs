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

namespace UtilityMaster.Views;

public partial class TrickDetailWindow : Window
{
    private readonly List<string> _imagePaths = new();

    public TrickDetailWindow(TrickEntity trick)
    {
        InitializeComponent();

        var typeText = trick.Type switch
        {
            "wallbang" => "Wallbang",
            "boost" => "Boost",
            "jump" => "Jump",
            "camp" => "Camp",
            _ => trick.Type
        };

        Title = typeText + " - " + trick.Name;
        TitleLabel.Text = trick.Name;
        TypeSideText.Text = typeText + " | " + trick.Side;
        CoordText.Text = "(" + trick.X.ToString("F0") + ", " + trick.Y.ToString("F0") + ")";

        LoadImages(trick.ImagesJson);

        if (!string.IsNullOrWhiteSpace(trick.VideoUrl))
        {
            VideoTitle.Visibility = Visibility.Visible;
            var url = trick.VideoUrl;
            if (File.Exists(url))
            {
                VideoPlayer.Source = new Uri(url);
                VideoPlayer.Visibility = Visibility.Visible;
                VideoControls.Visibility = Visibility.Visible;
            }
            else
            {
                VideoLink.Text = url;
                VideoLink.Visibility = Visibility.Visible;
            }
        }

        NotesTitle.Text = "Notes";
        NotesText.Text = trick.Notes ?? "(none)";
    }

    private void LoadImages(string imagesJson)
    {
        try
        {
            var paths = JsonSerializer.Deserialize<List<string>>(imagesJson ?? "[]");
            if (paths == null || paths.Count == 0) { ImagesTitle.Visibility = Visibility.Collapsed; return; }

            _imagePaths.Clear();
            _imagePaths.AddRange(paths.Where(File.Exists));
            if (_imagePaths.Count == 0) { ImagesTitle.Visibility = Visibility.Collapsed; return; }

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

    private void VideoLink_Click(object sender, MouseButtonEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(VideoLink.Text))
        {
            try { Process.Start(new ProcessStartInfo(VideoLink.Text) { UseShellExecute = true }); }
            catch { }
        }
    }

    private void VideoPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        VideoPlayer.Visibility = Visibility.Collapsed;
        VideoControls.Visibility = Visibility.Collapsed;
    }
}
