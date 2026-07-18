 namespace UtilityMaster.Models;

 public class TrickEntity
 {
     public Guid Id { get; set; } = Guid.NewGuid();
     public Guid ProfileId { get; set; }
     public string MapId { get; set; } = "";
     public string Name { get; set; } = "";
     public string Type { get; set; } = "boost";
     public string? Side { get; set; }
     public double X { get; set; }
     public double Y { get; set; }
     public string Floor { get; set; } = "default";
     public string ImagesJson { get; set; } = "[]";
     public string? VideoUrl { get; set; }
     public string? Notes { get; set; }
     public bool IsDefault { get; set; }
     public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

     public ProfileEntity? Profile { get; set; }
 }
