using System.Collections.Generic;
using UtilityMaster.Models;

namespace UtilityMaster.Services;

public static class MapCatalog
{
    public static readonly List<MapInfo> Maps = new()
    {
        new() { Id = "de_dust2", DisplayName = "Dust2", RadarPath = "Maps/de_dust2_radar_psd.png", HasLowerFloor = false, PosX = -2530, PosY = 3001, Scale = 4.66f },
        new() { Id = "de_mirage", DisplayName = "Mirage", RadarPath = "Maps/de_mirage_radar_psd.png", HasLowerFloor = false, PosX = -3231, PosY = 1861, Scale = 5f },
        new() { Id = "de_inferno", DisplayName = "Inferno", RadarPath = "Maps/de_inferno_radar_psd.png", HasLowerFloor = false, PosX = -2087, PosY = 3840, Scale = 4.9f },
        new() { Id = "de_nuke", DisplayName = "Nuke", RadarPath = "Maps/de_nuke_radar_psd.png", LowerRadarPath = "Maps/de_nuke_lower_radar_psd.png", HasLowerFloor = true, PosX = -3453, PosY = 2887, Scale = 6f },
        new() { Id = "de_ancient", DisplayName = "Ancient", RadarPath = "Maps/de_ancient_radar_psd.png", HasLowerFloor = false, PosX = -2953, PosY = 2164, Scale = 5f },
        new() { Id = "de_anubis", DisplayName = "Anubis", RadarPath = "Maps/de_anubis_radar_psd.png", HasLowerFloor = false, PosX = -2796, PosY = 3328, Scale = 5.22f },
        new() { Id = "de_cache", DisplayName = "Cache", RadarPath = "Maps/de_cache_radar_psd.png", HasLowerFloor = false, PosX = -2000, PosY = 3250, Scale = 5.5f },
        new() { Id = "de_overpass", DisplayName = "Overpass", RadarPath = "Maps/de_overpass_radar_psd.png", HasLowerFloor = false, PosX = -4831, PosY = 1781, Scale = 5.2f },
        new() { Id = "de_train", DisplayName = "Train", RadarPath = "Maps/de_train_radar_psd.png", LowerRadarPath = "Maps/de_train_lower_radar_psd.png", HasLowerFloor = true, PosX = -2308, PosY = 2078, Scale = 4.08f },
        new() { Id = "de_vertigo", DisplayName = "Vertigo", RadarPath = "Maps/de_vertigo_radar_psd.png", LowerRadarPath = "Maps/de_vertigo_lower_radar_psd.png", HasLowerFloor = true, PosX = -3168, PosY = 1762, Scale = 4f },
    };
}
