using UtilityMaster.Services;

namespace UtilityMaster.Tests;

public class MapCatalogTests
{
    [Fact]
    public void ContainsAllExpectedMaps()
    {
        var expected = new[]
        {
            "de_dust2", "de_mirage", "de_inferno", "de_nuke", "de_ancient",
            "de_anubis", "de_cache", "de_overpass", "de_train", "de_vertigo"
        };

        var ids = MapCatalog.Maps.Select(m => m.Id).ToHashSet();

        Assert.All(expected, id => Assert.Contains(id, ids));
    }

    [Fact]
    public void EveryMapHasRadarPath()
    {
        Assert.All(MapCatalog.Maps, m => Assert.False(string.IsNullOrWhiteSpace(m.RadarPath)));
    }

    [Fact]
    public void MapsWithLowerFloorExposeLowerRadar()
    {
        foreach (var map in MapCatalog.Maps.Where(m => m.HasLowerFloor))
        {
            Assert.False(string.IsNullOrWhiteSpace(map.LowerRadarPath));
        }
    }
}
