using System.Windows;
using System.Windows.Controls;
using UtilityMaster.Models;
using UtilityMaster.Services;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace UtilityMaster.Views;

public partial class CreateTrickWindow : Window
{
    private readonly Action<TrickEntity> _onCreate;
    private readonly double _x, _y;
    private readonly List<string> _imagePaths = new();
    private readonly string _imageDir;

    public CreateTrickWindow(double x, double y, Action<TrickEntity> onCreate)
    {
        _x = x; _y = y; _onCreate = onCreate;
        InitializeComponent();
        _imageDir = Services.DatabaseService.LineupImagesPath;
        Directory.CreateDirectory(_imageDir);
        PreviewKeyDown += OnKeyDown;
        ApplyLocalization();
        TypeBox.SelectionChanged += (_, _) => { UpdateSideState(); UpdateExtraFields(); };
    }

    private void ApplyLocalization()
    {
        Title = Loc.Get("create_trick.title");
        WinTitle.Text = Loc.Get("create_trick.title");
        NameLabel.Text = Loc.Get("create_trick.name");
        TypeLabel.Text = Loc.Get("create_trick.type");
        SideLabel.Text = Loc.Get("create_trick.side");
        VideoLabel.Text = Loc.Get("create_trick.video");
        NotesLabel.Text = Loc.Get("create_trick.notes");
        CreateBtn.Content = Loc.Get("create_trick.btn");
        CbWallbang.Content = Loc.Get("wallbang");
        CbBoost.Content = Loc.Get("boost");
        CbJump.Content = Loc.Get("jump");
        CbCamp.Content = Loc.Get("camp");
        CbT.Content = "T";
        CbCT.Content = "CT";
        CbBoth.Content = Loc.Get("create_trick.side_both");
        XLabel.Text = Loc.Get("create_trick.x");
        YLabel.Text = Loc.Get("create_trick.y");
        XBox.Text = _x.ToString("F0");
        YBox.Text = _y.ToString("F0");
    }

    private void TypeBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) { UpdateSideState(); UpdateExtraFields(); }

    private void UpdateExtraFields()
    {
        var sel = (TypeBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "wallbang";
        bool showExtra = sel is "boost" or "camp";
        ExtraFields.Visibility = showExtra ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateSideState()
    {
        var sel = (TypeBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "wallbang";
        bool canSide = sel is "wallbang" or "boost";
        SideBox.IsEnabled = canSide;
        SideLabel.Foreground = canSide
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x88, 0x88, 0x88))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x44, 0x44, 0x44));
        if (!canSide) SideBox.SelectedIndex = 2;
    }
    
    protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e) { base.OnMouseLeftButtonDown(e); if (e.OriginalSource is System.Windows.Controls.Border || e.OriginalSource is System.Windows.Controls.TextBlock) DragMove(); }

        public void PreSelectType(string type)
    {
        foreach (ComboBoxItem item in TypeBox.Items)
            if (item.Tag?.ToString() == type) { item.IsSelected = true; break; }
        UpdateSideState();
    }

    public void SetExistingValues(string name, string type, string side, double x, double y, string? videoUrl, string? notes, string? imagesJson = null)
    {
        NameBox.Text = name;
        XBox.Text = x.ToString("F0");
        YBox.Text = y.ToString("F0");
        VideoBox.Text = videoUrl ?? "";
        NotesBox.Text = notes ?? "";
        foreach (ComboBoxItem item in TypeBox.Items)
            if (item.Tag?.ToString() == type) { item.IsSelected = true; break; }
        foreach (ComboBoxItem item in SideBox.Items)
            if (item.Tag?.ToString() == side) { item.IsSelected = true; break; }
        Title = Loc.Get("edit");
        WinTitle.Text = Loc.Get("trick_edit.title");
        CreateBtn.Content = Loc.Get("add_lineup.save_btn");
            try { var paths = JsonSerializer.Deserialize<List<string>>(imagesJson ?? "[]"); if (paths != null && paths.Count > 0) { foreach (var p in paths) { if (File.Exists(p)) AddImage(p); } } } catch { }
        UpdateSideState();
    }

    private void OnKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control && Clipboard.ContainsImage())
        {
            PasteImage(); e.Handled = true;
        }
    }

    private void Paste_Click(object sender, RoutedEventArgs e) => PasteImage();

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
        catch (Exception ex) { MessageBox.Show(Loc.Get("add_lineup.paste_failed") + " " + ex.Message, Loc.Get("error"), MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void AddImage(string path)
    {
        _imagePaths.Add(path);
        var bmp = new BitmapImage();
        bmp.BeginInit(); bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.UriSource = new Uri(path); bmp.EndInit(); bmp.Freeze();
        var panel = new Border
        {
            Tag = path, Width = 100, Height = 80, Margin = new Thickness(2),
            Background = new SolidColorBrush(Color.FromRgb(0x0f, 0x11, 0x16)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x2a, 0x2d, 0x34)),
            BorderThickness = new Thickness(1), Cursor = Cursors.Hand,
            Child = new Image { Source = bmp, Stretch = Stretch.Uniform }
        };
        panel.MouseRightButtonDown += (s, e) =>
        {
            if (MessageBox.Show(Loc.Get("add_lineup.remove_image"), Loc.Get("add_lineup.remove_title"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _imagePaths.Remove(path); ImageList.Items.Remove(panel);
            }
            e.Handled = true;
        };
        ImageList.Items.Add(panel);
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (string.IsNullOrEmpty(name)) { ErrorLabel.Text = Loc.Get("create_trick.error_name"); ErrorLabel.Visibility = Visibility.Visible; return; }
        var type = (TypeBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "wallbang";
        var sideTag = (SideBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Both";
        var isBoostCamp = type is "boost" or "camp";
        if (!double.TryParse(XBox.Text, out var px) || !double.TryParse(YBox.Text, out var py))
        {
            ErrorLabel.Text = Loc.Get("create_trick.error_coord");
            ErrorLabel.Visibility = Visibility.Visible;
            return;
        }
        _onCreate(new TrickEntity
        {
            Name = name, Type = type,
            Side = (type is "wallbang" or "boost") ? sideTag : "Both",
            X = px, Y = py,
            VideoUrl = isBoostCamp ? VideoBox.Text.Trim() : "",
            Notes = isBoostCamp ? NotesBox.Text.Trim() : "",
            ImagesJson = isBoostCamp ? JsonSerializer.Serialize(_imagePaths) : "[]",
            IsDefault = false, CreatedAt = System.DateTime.UtcNow
        });
        Close();
    }
}
