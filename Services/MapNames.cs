namespace UtilityMaster.Services;

public static class MapNames
{
    public static string GetChineseName(string mapId) => mapId switch
    {
        "de_dust2" => "炙热沙城2",
        "de_mirage" => "荒漠迷城",
        "de_inferno" => "炼狱小镇",
        "de_nuke" => "核子危机",
        "de_ancient" => "远古遗迹",
        "de_anubis" => "阿努比斯",
        "de_cache" => "死城之谜",
        "de_overpass" => "死亡游乐园",
        "de_train" => "列车停放站",
        "de_vertigo" => "殒命大厦",
        _ => mapId
    };
}
