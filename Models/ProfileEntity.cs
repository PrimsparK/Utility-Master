 namespace UtilityMaster.Models;

 public class ProfileEntity
 {
     public Guid Id { get; set; } = Guid.NewGuid();
     public string Name { get; set; } = "";
     public bool IsDefault { get; set; }
     public bool AllowDeleteDefaultSpots { get; set; }
     public bool HasInheritedDefaults { get; set; }
     public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
     public string? ScreenshotBasePath { get; set; }
     public bool IsBuiltIn { get; set; }

     public ICollection<TargetEntity> Targets { get; set; } = new List<TargetEntity>();
     public ICollection<TrickEntity> Tricks { get; set; } = new List<TrickEntity>();
 }
