using System.Windows.Controls;
using System.Windows;
using UtilityMaster.Services;
using System;
using System.IO;

namespace UtilityMaster.Views;

public partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadSettings();
    }

    private void LoadSettings()
    {
        var s = SettingsService.Load();
        SetCombo(DefType, s.DefaultType);
        SetCombo(DefSide, s.DefaultSide);
        SetCombo(DefTrickType, s.DefaultTrickType);
        SetCombo(LangCombo, s.Language);
        ChkAutoPlay.IsChecked = s.AutoPlayVideo;
        ChkChineseTerms.IsChecked = s.UseChineseTerms;
        ChkAllowDelete.IsChecked = s.AllowDeleteDefaults;
        T_NadesTarget.Text = s.TargetConflictRadius.ToString();
        T_NadesLineup.Text = s.LineupConflictRadius.ToString();
        T_WallbangTarget.Text = s.WallbangConflictRadius.ToString();
        T_WallbangLineup.Text = s.WallbangLineupConflictRadius.ToString();
        T_TrickTarget.Text = s.TrickTargetConflictRadius.ToString();
        DataPath.Text = string.IsNullOrWhiteSpace(s.DataPath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UtilityMaster")
            : s.DataPath;
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        var s = SettingsService.Load();
        Loc.SetLanguage(s.Language);

        SettingsTitle.Text = Loc.Get("settings.title");
        DefaultFiltersHeader.Text = Loc.Get("settings.default_filters");
        LblDefType.Text = Loc.Get("settings.default_type");
        LblDefSide.Text = Loc.Get("settings.default_side");
        LblDefTrickType.Text = Loc.Get("settings.default_trick_type");
        ConflictHeader.Text = Loc.Get("settings.conflict_thresholds");
        LblNadesT.Text = Loc.Get("settings.nades_target");
        LblNadesL.Text = Loc.Get("settings.nades_lineup");
        LblWbT.Text = Loc.Get("settings.wallbang_target");
        LblWbL.Text = Loc.Get("settings.wallbang_lineup");
        LblTrickT.Text = Loc.Get("settings.tricks_target");
        DisplayHeader.Text = Loc.Get("settings.display");
        LblAutoPlay.Text = Loc.Get("settings.auto_play");
        LblLang.Text = Loc.Get("settings.language");
        LblChineseTerms.Text = Loc.Get("settings.chinese_terms");
        LblDataPathHeader.Text = Loc.Get("settings.storage");
        SecurityHeader.Text = Loc.Get("settings.security");
        LblAllowDelete.Text = Loc.Get("settings.allow_delete_defaults");
        SaveBtn.Content = Loc.Get("settings.save");

        // Translate DefType combo items
        DefTypeSmoke.Content = Loc.Get("smoke");
        DefTypeFlash.Content = Loc.Get("flash");
        DefTypeMolotov.Content = Loc.Get("molotov");
        DefTypeHE.Content = Loc.Get("he");

        // Translate DefTrickType combo items
        DefTrickWallbang.Content = Loc.Get("wallbang");
        DefTrickBoost.Content = Loc.Get("boost");
        DefTrickJump.Content = Loc.Get("jump");
        DefTrickCamp.Content = Loc.Get("camp");

        StatusText.Text = "";
        if (Window.GetWindow(this) is MainWindow mw) mw.Title = Loc.Get("window.title");
    }

    private void BrowseDataPath_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select data storage folder",
            Multiselect = false
        };
        if (dlg.ShowDialog() == true)
        {
            DataPath.Text = dlg.FolderName;
        }
    }

    private void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        var lang = GetCombo(LangCombo);
        var s = new SettingsData
        {
            DefaultType = GetCombo(DefType),
            DefaultSide = GetCombo(DefSide),
            DefaultTrickType = GetCombo(DefTrickType),
            Language = lang,
            AutoPlayVideo = ChkAutoPlay.IsChecked == true,
            UseChineseTerms = ChkChineseTerms.IsChecked == true,
            AllowDeleteDefaults = ChkAllowDelete.IsChecked == true,
            TargetConflictRadius = Parse(T_NadesTarget.Text, 20),
            LineupConflictRadius = Parse(T_NadesLineup.Text, 10),
            WallbangConflictRadius = Parse(T_WallbangTarget.Text, 20),
            WallbangLineupConflictRadius = Parse(T_WallbangLineup.Text, 10),
            TrickTargetConflictRadius = Parse(T_TrickTarget.Text, 15),
            DataPath = DataPath.Text.Trim(),
        };
        SettingsService.Save(s);
        Loc.SetLanguage(lang);
        if (Window.GetWindow(this) is MainWindow mw) mw.Title = Loc.Get("window.title");
        StatusText.Text = Loc.Get("settings.saved") + " (restart required for data path change)";
        ApplyLocalization();
    }

    private static void SetCombo(ComboBox cb, string tag)
    {
        foreach (ComboBoxItem item in cb.Items)
            if (item.Tag?.ToString() == tag) { item.IsSelected = true; return; }
    }
    private static string GetCombo(ComboBox cb) => (cb.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
    private static double Parse(string s, double def) => double.TryParse(s, out var v) ? v : def;
}
