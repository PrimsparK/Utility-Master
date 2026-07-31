using UtilityMaster.Services;

namespace UtilityMaster.Tests;

public class SettingsServiceTests
{
    [Fact]
    public void LoadMissingFileReturnsDefaults()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");

        var settings = SettingsService.Load(path);

        Assert.Equal("smoke", settings.DefaultType);
        Assert.Equal("Both", settings.DefaultSide);
        Assert.False(settings.AllowDeleteDefaults);
    }

    [Fact]
    public void SaveAndLoadRoundTripsValues()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
        var expected = new SettingsData
        {
            DefaultType = "he",
            DefaultSide = "CT",
            DefaultTrickType = "jump",
            Language = "zh",
            AutoPlayVideo = true,
            UseChineseTerms = true,
            AllowDeleteDefaults = true,
            TargetConflictRadius = 42,
            LineupConflictRadius = 9,
            DataPath = @"D:\tmp\utility-master"
        };

        SettingsService.Save(expected, path);
        var actual = SettingsService.Load(path);

        Assert.Equal(expected.DefaultType, actual.DefaultType);
        Assert.Equal(expected.DefaultSide, actual.DefaultSide);
        Assert.Equal(expected.DefaultTrickType, actual.DefaultTrickType);
        Assert.Equal(expected.Language, actual.Language);
        Assert.True(actual.AutoPlayVideo);
        Assert.True(actual.UseChineseTerms);
        Assert.True(actual.AllowDeleteDefaults);
        Assert.Equal(expected.TargetConflictRadius, actual.TargetConflictRadius);
        Assert.Equal(expected.LineupConflictRadius, actual.LineupConflictRadius);
        Assert.Equal(expected.DataPath, actual.DataPath);
    }
}
