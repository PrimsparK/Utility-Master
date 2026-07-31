namespace UtilityMaster.Models;

public class LineupSpot
{
    public double X { get; set; }
    public double Y { get; set; }
    public List<(TargetEntity Target, LineupEntity Lineup)> Entries { get; set; } = new();
}
