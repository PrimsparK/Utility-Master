using UtilityMaster.Models;

namespace UtilityMaster.Services;

public sealed record LineupSpotBuildResult(
    List<LineupSpot> Spots,
    List<TargetEntity> VisibleTargets);

public static class LineupSpotBuilder
{
    public static LineupSpotBuildResult Build(
        IEnumerable<TargetEntity> targets,
        IEnumerable<string> types,
        string side,
        string floor,
        bool proOnly)
    {
        var typeSet = types.ToHashSet();
        var entries = new List<(TargetEntity Target, LineupEntity Lineup)>();

        foreach (var target in targets)
        {
            if (!typeSet.Contains(target.Type) || target.Floor != floor) continue;
            if (proOnly && !target.Lineups.Any(l => l.IsPro)) continue;

            foreach (var lineup in target.Lineups)
            {
                if (lineup.Floor != floor) continue;
                var lineupSide = lineup.Side ?? "T";
                if (side != "Both" && lineupSide != side) continue;
                if (proOnly && !lineup.IsPro) continue;
                entries.Add((target, lineup));
            }
        }

        var visibleTargets = entries
            .Select(e => e.Target)
            .GroupBy(t => t.Id)
            .Select(g => g.First())
            .ToList();

        var spots = entries
            .GroupBy(e => (X: Math.Round(e.Lineup.X), Y: Math.Round(e.Lineup.Y)))
            .Select(g => new LineupSpot
            {
                X = g.Key.X,
                Y = g.Key.Y,
                Entries = g.ToList()
            })
            .ToList();

        return new LineupSpotBuildResult(spots, visibleTargets);
    }
}
