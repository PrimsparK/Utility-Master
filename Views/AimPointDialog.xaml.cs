using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace UtilityMaster.Views;

public partial class AimPointDialog : Window
{
    public bool Confirmed { get; private set; }
    public string? ImagePath { get; private set; }
    public string? Description => DescBox.Text;

    private readonly string _imageDir;

    public AimPointDialog(Window owner)
    {
        InitializeComponent();
        Owner = owner;
        _imageDir = Services.DatabaseService.LineupImagesPath;
        Directory.CreateDirectory(_imageDir);
    }

    private void PasteBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!Clipboard.ContainsImage()) return;
            var bitmap = Clipboard.GetImage();
            if (bitmap == null) return;

            var rtb = new RenderTargetBitmap(bitmap.PixelWidth, bitmap.PixelHeight, bitmap.DpiX, bitmap.DpiY, System.Windows.Media.PixelFormats.Pbgra32);
            var dv = new System.Windows.Media.DrawingVisual();
            using (var dc = dv.RenderOpen()) { dc.DrawImage(bitmap, new Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight)); }
            rtb.Render(dv);

            var savePath = Path.Combine(_imageDir, "aim_" + Guid.NewGuid() + ".png");
            using (var ms = new MemoryStream())
            {
                var enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(rtb));
                enc.Save(ms);
                ms.Position = 0;
                File.WriteAllBytes(savePath, ms.ToArray());
            }
            ImagePath = savePath;
            ImagePathBox.Text = savePath;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Paste failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OkBtn_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = true;
        Close();
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = false;
        Close();
    }
}
