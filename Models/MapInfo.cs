 namespace UtilityMaster.Models;

 public class MapInfo
 {
     public string Id { get; set; } = "";
     public string DisplayName { get; set; } = "";
     public string RadarPath { get; set; } = "";
     public string? LowerRadarPath { get; set; }
     public bool HasLowerFloor { get; set; }
     public float PosX { get; set; }
     public float PosY { get; set; }
     public float Scale { get; set; }
 }
