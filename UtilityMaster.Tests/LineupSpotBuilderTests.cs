using UtilityMaster.Models;
using UtilityMaster.Services;

namespace UtilityMaster.Tests;

public class LineupSpotBuilderTests
{
    private static TargetEntity Target(
        Guid id,
        string type,
        params LineupEntity[] lineups)
    {
        var target = new TargetEntity
        {
            Id = id,
            Type = type,
            Floor = "default",
            Side = "T",
            Name = type
        };
        foreach (var lineup in lineups) target.Lineups.Add(lineup);
        return target;
    }

    private static LineupEntity Lineup(
        double x,
        double y,
        string side = "T",
        string floor = "default",
        bool isPro = false)
    {
        return new LineupEntity
        {
            X = x,
            Y = y,
            Side = side,
            Floor = floor,
            IsPro = isPro
        };
    }

    [Fact]
    public void GroupsLineupsByRoundedCoordinate()
    {
        var targets = new[]
        {
            Target(Guid.NewGuid(), "smoke", Lineup(10.1, 20.1)),
            Target(Guid.NewGuid(), "flash", Lineup(10.4, 20.4))
        };

        var result = LineupSpotBuilder.Build(targets, new[] { "smoke", "flash" }, "Both", "default", proOnly: false);

        var spot = Assert.Single(result.Spots);
        Assert.Equal(10, spot.X);
        Assert.Equal(20, spot.Y);
        Assert.Equal(2, spot.Entries.Count);
    }

    [Fact]
    public void FiltersByUtilityType()
    {
        var targets = new[]
        {
            Target(Guid.NewGuid(), "smoke", Lineup(10, 10)),
            Target(Guid.NewGuid(), "he", Lineup(20, 20))
        };

        var result = LineupSpotBuilder.Build(targets, new[] { "he" }, "Both", "default", proOnly: false);

        var spot = Assert.Single(result.Spots);
        Assert.Single(spot.Entries);
        Assert.Equal("he", spot.Entries[0].Target.Type);
    }

    [Fact]
    public void FiltersByLineupSide()
    {
        var targets = new[]
        {
            Target(Guid.NewGuid(), "smoke", Lineup(10, 10, side: "T"), Lineup(20, 20, side: "CT"))
        };

        var result = LineupSpotBuilder.Build(targets, new[] { "smoke" }, "T", "default", proOnly: false);

        var spot = Assert.Single(result.Spots);
        Assert.Single(spot.Entries);
        Assert.Equal("T", spot.Entries[0].Lineup.Side);
    }

    [Fact]
    public void FiltersByFloor()
    {
        var targets = new[]
        {
            Target(Guid.NewGuid(), "smoke", Lineup(10, 10, floor: "default"), Lineup(20, 20, floor: "lower"))
        };

        var result = LineupSpotBuilder.Build(targets, new[] { "smoke" }, "Both", "default", proOnly: false);

        var spot = Assert.Single(result.Spots);
        Assert.Single(spot.Entries);
        Assert.Equal("default", spot.Entries[0].Lineup.Floor);
    }

    [Fact]
    public void FiltersByProFlag()
    {
        var targets = new[]
        {
            Target(Guid.NewGuid(), "smoke", Lineup(10, 10, isPro: true), Lineup(20, 20, isPro: false))
        };

        var result = LineupSpotBuilder.Build(targets, new[] { "smoke" }, "Both", "default", proOnly: true);

        var spot = Assert.Single(result.Spots);
        Assert.Single(spot.Entries);
        Assert.True(spot.Entries[0].Lineup.IsPro);
    }

    [Fact]
    public void VisibleTargetsAreDistinct()
    {
        var targetId = Guid.NewGuid();
        var targets = new[]
        {
            Target(targetId, "smoke", Lineup(10, 10), Lineup(20, 20))
        };

        var result = LineupSpotBuilder.Build(targets, new[] { "smoke" }, "Both", "default", proOnly: false);

        var visible = Assert.Single(result.VisibleTargets);
        Assert.Equal(targetId, visible.Id);
    }
}
