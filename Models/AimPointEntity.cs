namespace UtilityMaster.Models;

public class AimPointEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LineupId { get; set; }
    public int Sequence { get; set; }
    public string? ImagePath { get; set; }
    public string? Description { get; set; }

    public LineupEntity? Lineup { get; set; }
}
