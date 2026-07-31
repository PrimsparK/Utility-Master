using System.Windows.Media;
using UtilityMaster.Services;

namespace UtilityMaster.Tests;

public class MapRenderingHelpersTests
{
    [Theory]
    [InlineData("smoke", "T", "smoke.png")]
    [InlineData("flash", "T", "flash.png")]
    [InlineData("he", "T", "he.png")]
    [InlineData("molotov", "T", "molotov.png")]
    [InlineData("molotov", "CT", "incendiary.png")]
    [InlineData("wallbang", "T", "")]
    public void GetTargetIconFileReturnsExpectedFile(string type, string side, string expected)
    {
        Assert.Equal(expected, MapRenderingHelpers.GetTargetIconFile(type, side));
    }

    [Fact]
    public void GetNadeTypeColorReturnsDistinctColors()
    {
        var colors = new[]
        {
            MapRenderingHelpers.GetNadeTypeColor("smoke"),
            MapRenderingHelpers.GetNadeTypeColor("flash"),
            MapRenderingHelpers.GetNadeTypeColor("he"),
            MapRenderingHelpers.GetNadeTypeColor("molotov")
        };

        Assert.Equal(4, colors.Distinct().Count());
        Assert.NotEqual(Colors.Transparent, colors[0]);
    }

    [Fact]
    public void GetNadeTypeOrderPutsSmokeFirstAndMolotovLast()
    {
        Assert.Equal(0, MapRenderingHelpers.GetNadeTypeOrder("smoke"));
        Assert.Equal(3, MapRenderingHelpers.GetNadeTypeOrder("molotov"));
        Assert.Equal(4, MapRenderingHelpers.GetNadeTypeOrder("unknown"));
    }

    [Fact]
    public void GetNadeTypeLabelLocalizesMolotovBySide()
    {
        Loc.SetLanguage("en");

        Assert.Equal("Molotov", MapRenderingHelpers.GetNadeTypeLabel("molotov", "T"));
        Assert.Equal("Incendiary", MapRenderingHelpers.GetNadeTypeLabel("molotov", "CT"));
        Loc.SetLanguage("en");
    }
}
