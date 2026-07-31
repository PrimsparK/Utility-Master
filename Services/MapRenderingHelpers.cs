using System.Windows.Media;

namespace UtilityMaster.Services;

public static class MapRenderingHelpers
{
    public static string GetTargetIconFile(string type, string side)
    {
        return type switch
        {
            "smoke" => "smoke.png",
            "flash" => "flash.png",
            "he" => "he.png",
            "molotov" => side == "CT" ? "incendiary.png" : "molotov.png",
            "wallbang" => "",
            "jump" => "",
            _ => "smoke.png"
        };
    }

    public static Color GetNadeTypeColor(string type)
    {
        return type switch
        {
            "smoke" => Color.FromRgb(0x9a, 0xa7, 0xb8),
            "flash" => Color.FromRgb(0xf5, 0xd4, 0x42),
            "he" => Color.FromRgb(0x6b, 0xc0, 0x4b),
            "molotov" => Color.FromRgb(0xf0, 0x78, 0x18),
            _ => Color.FromRgb(0x88, 0x88, 0x88)
        };
    }

    public static int GetNadeTypeOrder(string type)
    {
        return type switch
        {
            "smoke" => 0,
            "flash" => 1,
            "he" => 2,
            "molotov" => 3,
            _ => 4
        };
    }

    public static string GetNadeTypeLabel(string type, string? side)
    {
        return type switch
        {
            "smoke" => Loc.Get("smoke"),
            "flash" => Loc.Get("flash"),
            "he" => Loc.Get("he"),
            "molotov" => side == "CT" ? Loc.Get("incendiary") : Loc.Get("molotov"),
            _ => type
        };
    }
}
