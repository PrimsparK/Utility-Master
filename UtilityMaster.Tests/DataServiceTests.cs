using UtilityMaster.Models;
using UtilityMaster.Services;
using UtilityMaster.Data;

namespace UtilityMaster.Tests;

public class DataServiceTests
{
    private static string NewDbPath()
    {
        return Path.Combine(Path.GetTempPath(), "UtilityMaster.Tests", Guid.NewGuid().ToString("N"));
    }

    private static TargetEntity AddTarget(
        DataService service,
        Guid profileId,
        string type = "smoke",
        string side = "T",
        string floor = "default",
        string mapId = "de_mirage")
    {
        var target = new TargetEntity
        {
            ProfileId = profileId,
            MapId = mapId,
            Name = type + " target",
            Type = type,
            Side = side,
            X = 100,
            Y = 200,
            Floor = floor
        };
        service.AddTarget(target);
        return target;
    }

    private static LineupEntity AddLineup(
        DataService service,
        Guid targetId,
        double x,
        double y,
        bool isPro = false,
        string side = "T",
        string floor = "default")
    {
        var lineup = new LineupEntity
        {
            TargetId = targetId,
            Name = "lineup",
            Side = side,
            Sequence = 1,
            X = x,
            Y = y,
            Floor = floor,
            IsPro = isPro
        };
        service.AddLineup(lineup);
        return lineup;
    }

    [Fact]
    public void CreateAndActivateProfileReturnsSameProfile()
    {
        using var service = new DataService(NewDbPath());

        var profile = service.CreateProfile("Test");
        service.SetActiveProfile(profile.Id);

        var active = service.GetActiveProfile();

        Assert.NotNull(active);
        Assert.Equal(profile.Id, active!.Id);
        Assert.Equal("Test", active.Name);
    }

    [Fact]
    public void GetAllTargetsIncludesLineups()
    {
        using var service = new DataService(NewDbPath());
        var profile = service.CreateProfile("Test");
        service.SetActiveProfile(profile.Id);
        var target = AddTarget(service, profile.Id);
        AddLineup(service, target.Id, 10, 20);
        AddLineup(service, target.Id, 30, 40);

        var loaded = service.GetAllTargets(profile.Id, target.MapId);

        var targetWithLineups = Assert.Single(loaded);
        Assert.Equal(2, targetWithLineups.Lineups.Count);
    }

    [Fact]
    public void GetTargetIncludesLineupsAndAimPoints()
    {
        var dbPath = NewDbPath();
        var db = DatabaseService.CreateContext(dbPath);
        using var service = new DataService(db);
        var profile = service.CreateProfile("Test");
        service.SetActiveProfile(profile.Id);
        var target = AddTarget(service, profile.Id);
        var lineup = AddLineup(service, target.Id, 10, 20);

        db.AimPoints.Add(new AimPointEntity
        {
            LineupId = lineup.Id,
            Sequence = 1,
            Description = "crosshair"
        });
        db.SaveChanges();

        var loaded = service.GetTarget(target.Id);

        Assert.NotNull(loaded);
        var loadedLineup = Assert.Single(loaded!.Lineups);
        var aimPoint = Assert.Single(loadedLineup.AimPoints);
        Assert.Equal("crosshair", aimPoint.Description);
    }

    [Fact]
    public void DeleteLineupResequencesRemaining()
    {
        using var service = new DataService(NewDbPath());
        var profile = service.CreateProfile("Test");
        service.SetActiveProfile(profile.Id);
        var target = AddTarget(service, profile.Id);

        var first = new LineupEntity { TargetId = target.Id, Sequence = 1, X = 10, Y = 10, Floor = "default" };
        var second = new LineupEntity { TargetId = target.Id, Sequence = 2, X = 20, Y = 20, Floor = "default" };
        var third = new LineupEntity { TargetId = target.Id, Sequence = 3, X = 30, Y = 30, Floor = "default" };
        service.AddLineup(first);
        service.AddLineup(second);
        service.AddLineup(third);

        service.DeleteLineup(second.Id);

        var remaining = service.GetLineupsQuery(target.Id);
        Assert.Equal(new[] { 1, 2 }, remaining.Select(l => l.Sequence).ToArray());
    }

    [Fact]
    public void GetTargetsAppliesTypeSideAndProFilters()
    {
        using var service = new DataService(NewDbPath());
        var profile = service.CreateProfile("Test");
        service.SetActiveProfile(profile.Id);

        var smokeT = AddTarget(service, profile.Id, "smoke", "T");
        AddLineup(service, smokeT.Id, 10, 10, isPro: true);

        var smokeCT = AddTarget(service, profile.Id, "smoke", "CT");
        AddLineup(service, smokeCT.Id, 20, 20);

        var flash = AddTarget(service, profile.Id, "flash", "T");
        AddLineup(service, flash.Id, 30, 30);

        var smokeOnly = service.GetTargets(profile.Id, smokeT.MapId, "smoke", "Both", "default", proOnly: false);
        var proSmoke = service.GetTargets(profile.Id, smokeT.MapId, "smoke", "Both", "default", proOnly: true);
        var tOnly = service.GetTargets(profile.Id, smokeT.MapId, "smoke", "T", "default", proOnly: false);

        Assert.Equal(2, smokeOnly.Count);
        Assert.Single(proSmoke);
        Assert.Equal(smokeT.Id, proSmoke[0].Id);
        Assert.Single(tOnly);
        Assert.Equal(smokeT.Id, tOnly[0].Id);
    }

    [Fact]
    public void DeleteTargetRemovesItsLineups()
    {
        using var service = new DataService(NewDbPath());
        var profile = service.CreateProfile("Test");
        service.SetActiveProfile(profile.Id);
        var target = AddTarget(service, profile.Id);
        var lineup = AddLineup(service, target.Id, 10, 20);

        service.DeleteTarget(target.Id);

        Assert.Empty(service.GetAllTargets(profile.Id, target.MapId));
        Assert.Empty(service.GetLineupsQuery(target.Id));
    }

    [Fact]
    public void UpdateLineupPersistsChanges()
    {
        using var service = new DataService(NewDbPath());
        var profile = service.CreateProfile("Test");
        service.SetActiveProfile(profile.Id);
        var target = AddTarget(service, profile.Id);
        var lineup = AddLineup(service, target.Id, 10, 20);

        lineup.Name = "updated";
        lineup.AimDescription = "aim here";
        lineup.IsPro = true;
        service.UpdateLineup(lineup);

        var loaded = service.GetLineup(lineup.Id);
        Assert.NotNull(loaded);
        Assert.Equal("updated", loaded!.Name);
        Assert.Equal("aim here", loaded.AimDescription);
        Assert.True(loaded.IsPro);
    }

    [Fact]
    public void AddAndDeleteTrickRoundTrips()
    {
        using var service = new DataService(NewDbPath());
        var profile = service.CreateProfile("Test");
        service.SetActiveProfile(profile.Id);

        var trick = new TrickEntity
        {
            ProfileId = profile.Id,
            MapId = "de_mirage",
            Name = "Boost A",
            Type = "boost",
            Side = "CT",
            X = 1,
            Y = 2,
            Floor = "default"
        };
        service.AddTrick(trick);

        var loaded = service.GetTricks(profile.Id, trick.MapId);
        var single = Assert.Single(loaded);
        Assert.Equal("Boost A", single.Name);

        service.DeleteTrick(trick.Id);
        Assert.Empty(service.GetTricks(profile.Id, trick.MapId));
    }

    [Fact]
    public void UpdateTrickPersistsChanges()
    {
        using var service = new DataService(NewDbPath());
        var profile = service.CreateProfile("Test");
        service.SetActiveProfile(profile.Id);
        var trick = new TrickEntity
        {
            ProfileId = profile.Id,
            MapId = "de_mirage",
            Name = "Old",
            Type = "boost",
            X = 1,
            Y = 2
        };
        service.AddTrick(trick);

        trick.Name = "New";
        trick.Notes = "notes";
        service.UpdateTrick(trick);

        var loaded = service.GetTrick(trick.Id);
        Assert.NotNull(loaded);
        Assert.Equal("New", loaded!.Name);
        Assert.Equal("notes", loaded.Notes);
    }

    [Fact]
    public void GetAllProfilesReturnsCreatedProfiles()
    {
        using var service = new DataService(NewDbPath());
        var first = service.CreateProfile("First");
        var second = service.CreateProfile("Second");

        var profiles = service.GetAllProfiles();

        Assert.Contains(profiles, p => p.Id == first.Id);
        Assert.Contains(profiles, p => p.Id == second.Id);
    }
}
