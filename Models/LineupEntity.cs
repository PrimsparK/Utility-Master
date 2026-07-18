namespace UtilityMaster.Models;

    public class LineupEntity
    {
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TargetId { get; set; }
    public string Name { get; set; } = "";
    public string Side { get; set; } = "T";
    public int Sequence { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public string Floor { get; set; } = "default";
    public string StandImagesJson { get; set; } = "[]";
    public string AimImagesJson { get; set; } = "[]";
    public string ImagesJson { get; set; } = "[]";
    public string? AimDescription { get; set; }
    public string? ThrowType { get; set; }
    public string? VideoUrl { get; set; }
    public string? Notes { get; set; }
    public bool IsDefault { get; set; }
    public bool IsPro { get; set; }
    public Guid? GroupId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TargetEntity? Target { get; set; }
    public ICollection<AimPointEntity> AimPoints { get; set; } = new List<AimPointEntity>();
    }