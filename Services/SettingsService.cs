using System.Text.Json;
using System.IO;

namespace UtilityMaster.Services;

public class SettingsData
{
    public string DefaultType { get; set; } = "smoke";
    public string DefaultSide { get; set; } = "Both";
    public string DefaultTrickType { get; set; } = "wallbang";
    public bool AutoPlayVideo { get; set; }
    public string Language { get; set; } = "en";
    public bool UseChineseTerms { get; set; }
    public double TargetConflictRadius { get; set; } = 20;
    public double LineupConflictRadius { get; set; } = 10;
    public double WallbangConflictRadius { get; set; } = 20;
    public double WallbangLineupConflictRadius { get; set; } = 10;
    public double TrickTargetConflictRadius { get; set; } = 15;
    public bool AllowDeleteDefaults { get; set; }
    public string DataPath { get; set; } = "";
}

public static class SettingsService
{
    private static string DefaultFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "UtilityMaster", "settings.json");

    public static SettingsData Load(string? filePath = null)
    {
        try
        {
            var path = filePath ?? DefaultFilePath;
            if (File.Exists(path))
                return JsonSerializer.Deserialize<SettingsData>(File.ReadAllText(path)) ?? new();
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
        return new SettingsData();
    }

    public static void Save(SettingsData data, string? filePath = null)
    {
        var path = filePath ?? DefaultFilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
    }
}

