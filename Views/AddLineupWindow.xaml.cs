using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UtilityMaster.Services;

namespace UtilityMaster.Views;

public partial class AddLineupWindow : Window
{
    public bool Confirmed { get; private set; }
    public Action<AddLineupWindow>? CloseCallback { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public string AimDescription => AimDesc.Text;
    public string ThrowTypeValue => ((ComboBoxItem)ThrowType.SelectedItem)?.Tag?.ToString() ?? "standing";
    public string VideoUrlValue => VideoUrl.Text;
    public string NotesValue => Notes.Text;
    public bool WantsPick { get; set; }
    public bool IsPro { get; set; }
    private readonly List<string> _imagePaths = new();
    public string ImagesJson => JsonSerializer.Serialize(_imagePaths);
    public string LineupNameValue => LineupName.Text.Trim();
    public string SideValue => ((ComboBoxItem)LineupSide.SelectedItem)?.Tag?.ToString() ?? "T";
    private readonly string _imageDir;

    // Position pick state

    public AddLineupWindow(double x, double y)
    {
        InitializeComponent();
        X = x; Y = y;
        LineupX.Text = x.ToString("F0");
        LineupY.Text = y.ToString("F0");

        _imageDir = DatabaseService.LineupImagesPath;
        Directory.CreateDirectory(_imageDir);

        PreviewKeyDown += (s, e) =>
        {
            if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (Clipboard.ContainsImage())
                {
                    PasteImage();
                    e.Handled = true;
                }
            }
        };

        Loaded += OnLoaded;
        ApplyLocalization();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // No mini-map, pick position via map overlay
    }

    // Pick position on the map via overlay
    private void PickPosBtn_Click(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(LineupX.Text, out var cx)) X = cx;
        if (double.TryParse(LineupY.Text, out var cy)) Y = cy;

        var mainWin = Application.Current.MainWindow;
        if (mainWin is MainWindow mw && mw.ContentFrame.Content is MapView mv)
        {
            Opacity = 0;
            mv.EnterPositionPickMode(X, Y, (newX, newY) =>
            {
                Dispatcher.Invoke(() =>
                {
                    X = newX; Y = newY;
                    LineupX.Text = newX.ToString("F0");
                    LineupY.Text = newY.ToString("F0");
                    Opacity = 1;
                    Activate();
                });
            });
        }
    }


    private void ApplyLocalization()
    {
        Title = Loc.Get("add_lineup.title");
        WinTitle.Text = Loc.Get("add_lineup.title");
        L_Name.Text = Loc.Get("add_lineup.name");
        L_Side.Text = Loc.Get("add_lineup.side");
        L_X.Text = Loc.Get("add_lineup.x");
        L_Y.Text = Loc.Get("add_lineup.y");
        L_Aim.Text = Loc.Get("add_lineup.aim");
        L_Throw.Text = Loc.Get("add_lineup.throw_type");
        L_Video.Text = Loc.Get("add_lineup.video");
        L_Notes.Text = Loc.Get("add_lineup.notes");
        L_Images.Text = Loc.Get("add_lineup.images");
        PasteBtn.Content = Loc.Get("add_lineup.paste_btn");
        PickPosBtn.Content = Loc.Get("add_lineup.pick_btn");
        AddBtn.Content = Loc.Get("add_lineup.btn");
        CbStanding.Content = Loc.Get("standing");
        CbJumpThrow.Content = Loc.Get("jump_throw");
        CbRunning.Content = Loc.Get("running");
        ProCheck.Content = Loc.Get("add_lineup.pro");
    }

    private void ProCheck_Changed(object sender, RoutedEventArgs e)
    {
        IsPro = ProCheck.IsChecked == true;
    }

    public void UpdateCoordDisplay()
    {
        LineupX.Text = X.ToString("F0");
        LineupY.Text = Y.ToString("F0");
    }

    protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e) { base.OnMouseLeftButtonDown(e); if (e.OriginalSource is System.Windows.Controls.Border || e.OriginalSource is System.Windows.Controls.TextBlock) DragMove(); }

    public void PreFill(string aimDesc, string throwType, string videoUrl, string notes)
    {
        PreFillFull("", "T", aimDesc, throwType, videoUrl, notes);
    }

    public void PreFillFull(string name, string side, string aimDesc, string throwType, string videoUrl, string notes)
    {
        LineupName.Text = name;
        foreach (ComboBoxItem item in LineupSide.Items)
            if (item.Tag?.ToString() == side) { item.IsSelected = true; break; }
        AimDesc.Text = aimDesc;
        VideoUrl.Text = videoUrl;
        Notes.Text = notes;
        foreach (ComboBoxItem item in ThrowType.Items)
            if (item.Tag?.ToString() == throwType) { item.IsSelected = true; break; }
        WinTitle.Text = Loc.Get("add_lineup.edit_title");
        AddBtn.Content = Loc.Get("add_lineup.save_btn");
        Title = Loc.Get("add_lineup.edit_title");
    }

    public void SetExistingImages(List<string> paths)
    {
        foreach (var p in paths)
            if (File.Exists(p)) AddImage(p);
    }

    private void PasteImageBtn_Click(object sender, RoutedEventArgs e) => PasteImage();

    private void PasteImage()
    {
        try
        {
            if (!Clipboard.ContainsImage()) return;
            var bitmap = Clipboard.GetImage();
            if (bitmap == null) return;

            var rtb = new RenderTargetBitmap(bitmap.PixelWidth, bitmap.PixelHeight, bitmap.DpiX, bitmap.DpiY, PixelFormats.Pbgra32);
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen()) { dc.DrawImage(bitmap, new Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight)); }
            rtb.Render(dv);

            var savePath = Path.Combine(_imageDir, Guid.NewGuid() + ".png");
            using (var ms = new MemoryStream())
            {
                var enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(rtb));
                enc.Save(ms);
                ms.Position = 0;
                File.WriteAllBytes(savePath, ms.ToArray());
            }
            AddImage(savePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(Loc.Get("add_lineup.paste_failed") + " " + ex.Message, Loc.Get("error"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddImage(string path)
    {
        _imagePaths.Add(path);
        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.UriSource = new Uri(path);
        bmp.EndInit();
        bmp.Freeze();

        var panel = new Border
        {
            Tag = path,
            Width = 100, Height = 80, Margin = new Thickness(2),
            Background = new SolidColorBrush(Color.FromRgb(0x0f, 0x11, 0x16)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x2a, 0x2d, 0x34)),
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Child = new Image { Source = bmp, Stretch = Stretch.Uniform }
        };
        panel.MouseRightButtonDown += (s, e) =>
        {
            if (MessageBox.Show(Loc.Get("add_lineup.remove_image"), Loc.Get("add_lineup.remove_title"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _imagePaths.Remove(path);
                ImageList.Items.Remove(panel);
            }
            e.Handled = true;
        };
        ImageList.Items.Add(panel);
    }

    private void Coord_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (double.TryParse(LineupX.Text, out var vx)) X = vx;
        if (double.TryParse(LineupY.Text, out var vy)) Y = vy;
    }

    private void VideoUrl_TextChanged(object sender, TextChangedEventArgs e)
    {
        var url = VideoUrl.Text.Trim();
        if (string.IsNullOrEmpty(url)) { VideoPlayer.Visibility = Visibility.Collapsed; VideoControls.Visibility = Visibility.Collapsed; return; }
        if (File.Exists(url))
        {
            VideoPlayer.Source = new Uri(url);
            VideoPlayer.Visibility = Visibility.Visible;
            VideoControls.Visibility = Visibility.Visible;
        }
        else
        {
            VideoPlayer.Visibility = Visibility.Collapsed;
            VideoControls.Visibility = Visibility.Collapsed;
        }
    }

    private void VideoPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        VideoPlayer.Visibility = Visibility.Collapsed;
        VideoControls.Visibility = Visibility.Collapsed;
    }

    private void VideoPlay_Click(object sender, RoutedEventArgs e) => VideoPlayer.Play();
    private void VideoPause_Click(object sender, RoutedEventArgs e) => VideoPlayer.Pause();
    private void VideoStop_Click(object sender, RoutedEventArgs e) => VideoPlayer.Stop();

    private void CloseBtn_Click(object sender, RoutedEventArgs e) {
        if (CloseCallback != null) { CloseCallback(this); }
        else { Close(); }
    }

    private void AddBtn_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(LineupName.Text))
        {
            ErrorText.Text = Loc.Get("add_lineup.error_name");
            ErrorText.Visibility = Visibility.Visible;
            return;
        }
        if (!double.TryParse(LineupX.Text, out var px) || !double.TryParse(LineupY.Text, out var py))
        { ErrorText.Text = Loc.Get("add_lineup.error_coord"); ErrorText.Visibility = Visibility.Visible; return; }
        X = px; Y = py;
        Confirmed = true;
        if (CloseCallback != null) { CloseCallback(this); }
        else { Close(); }
    }
}







