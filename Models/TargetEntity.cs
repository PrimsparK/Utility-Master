 namespace UtilityMaster.Models;

 public class TargetEntity
 {
     public Guid Id { get; set; } = Guid.NewGuid();
     public Guid ProfileId { get; set; }
     public string MapId { get; set; } = "";
     public string Name { get; set; } = "";
     public string Type { get; set; } = "smoke";
     public string Side { get; set; } = "T";
     public double X { get; set; }
     public double Y { get; set; }
     public string Floor { get; set; } = "default";
     public string? Image { get; set; }
     public bool IsDefault { get; set; }
     public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

     public ProfileEntity? Profile { get; set; }
     public ICollection<LineupEntity> Lineups { get; set; } = new List<LineupEntity>();
 }
