using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using UtilityMaster.Services;

namespace UtilityMaster.Views;

public partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        SubtitleText.Text = Loc.Get("about.subtitle");
        LinksTitle.Text = Loc.Get("about.links");
        LicenseTitle.Text = Loc.Get("about.license");
        LicenseText.Text = Loc.Get("about.license_text");
        CreditsTitle.Text = Loc.Get("about.credits");
        Credit1Prefix.Text = Loc.Get("about.credit1");
        Credit2Prefix.Text = Loc.Get("about.credit2");
        DisclaimersTitle.Text = Loc.Get("about.disclaimers");
        Disclaimer1.Text = Loc.Get("about.disclaimer1");
        Disclaimer2.Text = Loc.Get("about.disclaimer2");
        Disclaimer3.Text = Loc.Get("about.disclaimer3");
        Disclaimer4.Text = Loc.Get("about.disclaimer4");
        VersionText.Text = Loc.Get("about.version");
    }

    private void OpenUrl(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBlock tb && tb.Tag is string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { }
        }
    }

    private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
