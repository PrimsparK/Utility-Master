using System.Windows;
using System.Windows.Controls;
using UtilityMaster.Models;
using UtilityMaster.Services;

namespace UtilityMaster.Views;

public partial class CreateTargetWindow : Window
{
    private readonly Action<TargetEntity> _onCreated;
    private string _context = "nades";
    private string _existingName = "";

    public CreateTargetWindow(double x, double y, Action<TargetEntity> onCreated)
    {
        _onCreated = onCreated;
        InitializeComponent();
        Title = Loc.Get("create_target.title");
        WinTitleLabel.Text = Loc.Get("create_target.title");
        T_TypeLabel.Text = Loc.Get("create_target.type");
        T_SideLabel.Text = Loc.Get("create_target.side");
        T_XLabel.Text = Loc.Get("create_target.x");
        T_YLabel.Text = Loc.Get("create_target.y");
        CreateBtn.Content = Loc.Get("create_target.btn");
        TargetX.Text = x.ToString("F0");
        TargetY.Text = y.ToString("F0");
        CbSmoke.Content = Loc.Get("smoke"); CbFlash.Content = Loc.Get("flash");
        CbMolotov.Content = Loc.Get("molotov"); CbHE.Content = Loc.Get("he");
        CbSideBoth.Content = Loc.Get("create_trick.side_both");
        SelectSide(SettingsService.Load().DefaultSide);
    }

    public void SetTrickContext(string context)
    {
        _context = context;
        NadesTypePanel.Visibility = Visibility.Collapsed;
        foreach (ComboBoxItem item in TrickTypeCombo.Items)
        {
            item.Content = item.Tag?.ToString() switch
            {
                "wallbang" => Loc.Get("wallbang"),
                "boost" => Loc.Get("boost"),
                "jump" => Loc.Get("jump"),
                "camp" => Loc.Get("camp"),
                _ => item.Content
            };
        }
        TricksTypePanel.Visibility = Visibility.Visible;

        var tc = TrickTypeCombo;
        foreach (ComboBoxItem item in tc.Items)
            if (item.Tag?.ToString() == context) { item.IsSelected = true; break; }
        tc.IsEnabled = true;

        T_TrickTypeLabel.Text = Loc.Get("create_trick.type");
        T_SideLabel.Text = Loc.Get("create_trick.side");
        T_XLabel.Text = Loc.Get("create_trick.x");
        T_YLabel.Text = Loc.Get("create_trick.y");
        CreateBtn.Content = Loc.Get("create_target.btn");
        WinTitleLabel.Text = Loc.Get("create_target.title_trick");
        Title = WinTitleLabel.Text;
        CbSideBoth.Content = Loc.Get("create_trick.side_both");

        foreach (ComboBoxItem item in TrickTypeCombo.Items)
            if (item.Tag?.ToString() is "boost" or "camp") item.IsEnabled = false;

        UpdateButtonText();
        tc.SelectionChanged += (_, _) => UpdateButtonText();
    }

    private string _selectedTrickType = "wallbang";

    private void UpdateButtonText()
    {
        _selectedTrickType = (TrickTypeCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "wallbang";
        if (_context != "nades" && _selectedTrickType is "boost" or "camp")
        {
            foreach (ComboBoxItem item in TrickTypeCombo.Items)
                if (item.Tag?.ToString() == "wallbang") { item.IsSelected = true; return; }
        }
        if (_context != "nades" && _selectedTrickType is "jump" or "camp") SelectSide("Both");
        TargetSide.IsEnabled = _context == "nades" || _selectedTrickType == "wallbang";
        CreateBtn.Content = _selectedTrickType switch
        {
            "wallbang" => Loc.Get("create_target.btn"),
            "jump" => Loc.Get("create_target.btn"),
            "boost" => Loc.Get("create_trick.btn"),
            "camp" => Loc.Get("create_trick.btn"),
            _ => Loc.Get("create_target.btn")
        };
    }

    public void SetExistingValues(string name, string type, string side, double x, double y, string? image)
    {
        _existingName = name;
        SelectSide(side);
        TargetX.Text = x.ToString("F0");
        TargetY.Text = y.ToString("F0");
        if (_context == "nades")
        {
            foreach (ComboBoxItem item in TargetType.Items)
                if (item.Tag?.ToString() == type) { item.IsSelected = true; break; }
        }
        else
        {
            foreach (ComboBoxItem item in TrickTypeCombo.Items)
                if (item.Tag?.ToString() == type) { item.IsSelected = true; break; }
        }
        Title = Loc.Get("edit");
        WinTitleLabel.Text = Loc.Get("edit");
        CreateBtn.Content = Loc.Get("add_lineup.save_btn");
    }

    public void PreSelectType(string type)
    {
        foreach (ComboBoxItem item in TargetType.Items)
            if (item.Tag?.ToString() == type) { item.IsSelected = true; return; }
    }

    private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

    private void CreateBtn_Click(object sender, RoutedEventArgs e)
    {
        if (!double.TryParse(TargetX.Text, out var px) || !double.TryParse(TargetY.Text, out var py))
        { ErrorText.Text = Loc.Get("create_target.error_coord"); ErrorText.Visibility = Visibility.Visible; return; }

        string selType;
        if (_context == "nades")
            selType = ((ComboBoxItem)TargetType.SelectedItem)?.Tag?.ToString() ?? "smoke";
        else
            selType = _selectedTrickType;
        if (_context != "nades" && selType is not ("wallbang" or "jump")) selType = "wallbang";

        var side = ((ComboBoxItem)TargetSide.SelectedItem)?.Tag?.ToString() ?? "T";
        if (_context != "nades" && selType is "jump" or "camp") side = "Both";

        var typeLabel = selType switch
        {
            "smoke" => Loc.Get("smoke"),
            "flash" => Loc.Get("flash"),
            "he" => Loc.Get("he"),
            "molotov" => Loc.Get("molotov"),
            "wallbang" => Loc.Get("wallbang"),
            "jump" => Loc.Get("jump"),
            "boost" => Loc.Get("boost"),
            "camp" => Loc.Get("camp"),
            _ => selType
        };

        var target = new TargetEntity
        {
            Name = string.IsNullOrWhiteSpace(_existingName) ? $"{typeLabel} ({px:F0},{py:F0})" : _existingName,
            Type = selType,
            Side = side,
            X = px, Y = py,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };

        _onCreated(target);
        Close();
    }

    private void SelectSide(string side)
    {
        foreach (ComboBoxItem item in TargetSide.Items)
            if (item.Tag?.ToString() == side) { item.IsSelected = true; return; }
    }

    protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        if (e.OriginalSource is Border || e.OriginalSource is TextBlock)
            DragMove();
    }
}
