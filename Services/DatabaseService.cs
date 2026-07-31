using Microsoft.EntityFrameworkCore;
using UtilityMaster.Data;
using UtilityMaster.Models;
using System.Text.Json;
 using System.IO;

namespace UtilityMaster.Services;

 public static class DatabaseService
 {
    public static string GetBasePath()
    {
        var sets = SettingsService.Load();
        var path = string.IsNullOrWhiteSpace(sets.DataPath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UtilityMaster")
            : sets.DataPath;
        Directory.CreateDirectory(path);
        return path;
    }

    private static string DbPath => Path.Combine(GetBasePath(), "data.db");

    public static string LineupImagesPath => Path.Combine(GetBasePath(), "LineupImages");

     private static string ConfigJsonPath => Path.Combine(
         AppDomain.CurrentDomain.BaseDirectory, "Assets", "default_config.json");

     public static AppDbContext CreateContext()
     {
         var dir = Path.GetDirectoryName(DbPath)!;
         Directory.CreateDirectory(dir);

         var options = new DbContextOptionsBuilder<AppDbContext>()
             .UseSqlite($"Data Source={DbPath}")
             .Options;
         var ctx = new AppDbContext(options);
         ctx.Database.EnsureCreated();
         return ctx;
     }

     public static void InitializeDefaults(AppDbContext db)
     {
         if (db.Profiles.Any()) return;

         var profile = new ProfileEntity
         {
             Name = "Default",
             IsBuiltIn = true,
             HasInheritedDefaults = true,
             AllowDeleteDefaultSpots = false,
             CreatedAt = DateTime.UtcNow
         };
         db.Profiles.Add(profile);

         if (File.Exists(ConfigJsonPath))
         {
             var json = File.ReadAllText(ConfigJsonPath);
             var config = JsonSerializer.Deserialize<DefaultConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
             if (config?.Maps != null)
             {
                 foreach (var (mapId, mapData) in config.Maps)
                 {
                     if (mapData.Targets != null)
                     {
                         foreach (var t in mapData.Targets)
                         {
                             var target = new TargetEntity
                             {
                                 ProfileId = profile.Id, MapId = mapId,
                                 Name = t.Name, Type = t.Type, Side = t.Side,
                                 X = t.X, Y = t.Y, Floor = t.Floor ?? "default",
                                 Image = t.Image, IsDefault = true,
                                 CreatedAt = DateTime.UtcNow
                             };
                             db.Targets.Add(target);

                             if (t.Lineups != null)
                             {
                                 for (int i = 0; i < t.Lineups.Count; i++)
                                 {
                                     var l = t.Lineups[i];
                                     db.Lineups.Add(new LineupEntity
                                     {
                                         TargetId = target.Id, Sequence = i + 1,
                                         X = l.X, Y = l.Y, Floor = l.Floor ?? "default",
                                         StandImagesJson = JsonSerializer.Serialize(l.StandImages ?? new List<string>()),
                                         AimImagesJson = JsonSerializer.Serialize(l.AimImages ?? new List<string>()),
                                         AimDescription = l.AimDescription,
                                         ThrowType = l.ThrowType, VideoUrl = l.VideoUrl,
                                         Notes = l.Notes, IsDefault = true,
                                         CreatedAt = DateTime.UtcNow
                                     });
                                 }
                             }
                         }
                    }
                }
            }
            // Import default tricks (wallbang/jump -> Target+Lineup, boost/camp -> TrickEntity)
            if (config?.Maps != null)
            {
                foreach (var (mapId, mapData) in config.Maps)
                {
                    if (mapData.Tricks == null) continue;
                    if (mapData.Tricks.Wallbang != null)
                    {
                        foreach (var t in mapData.Tricks.Wallbang)
                        {
                            var target = new TargetEntity
                            {
                                ProfileId = profile.Id, MapId = mapId,
                                Name = t.Name, Type = "wallbang",
                                Side = t.Side ?? "Both", X = t.X, Y = t.Y,
                                Floor = t.Floor ?? "default", IsDefault = true,
                                CreatedAt = DateTime.UtcNow
                            };
                            db.Targets.Add(target);
                            db.Lineups.Add(new LineupEntity
                            {
                                TargetId = target.Id, Name = t.Name,
                                Sequence = 1, X = t.X, Y = t.Y,
                                Floor = t.Floor ?? "default",
                                ImagesJson = JsonSerializer.Serialize(t.Images ?? new List<string>()),
                                VideoUrl = t.VideoUrl, Notes = t.Notes,
                                IsDefault = true, CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                    if (mapData.Tricks.Jump != null)
                    {
                        foreach (var t in mapData.Tricks.Jump)
                        {
                            var target = new TargetEntity
                            {
                                ProfileId = profile.Id, MapId = mapId,
                                Name = t.Name, Type = "jump",
                                Side = t.Side ?? "Both", X = t.X, Y = t.Y,
                                Floor = t.Floor ?? "default", IsDefault = true,
                                CreatedAt = DateTime.UtcNow
                            };
                            db.Targets.Add(target);
                            db.Lineups.Add(new LineupEntity
                            {
                                TargetId = target.Id, Name = t.Name,
                                Sequence = 1, X = t.X, Y = t.Y,
                                Floor = t.Floor ?? "default",
                                ImagesJson = JsonSerializer.Serialize(t.Images ?? new List<string>()),
                                VideoUrl = t.VideoUrl, Notes = t.Notes,
                                IsDefault = true, CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                    if (mapData.Tricks.Boost != null)
                    {
                        foreach (var t in mapData.Tricks.Boost)
                        {
                            db.Tricks.Add(new TrickEntity
                            {
                                ProfileId = profile.Id, MapId = mapId,
                                Name = t.Name, Type = "boost",
                                Side = t.Side ?? "Both", X = t.X, Y = t.Y,
                                Floor = t.Floor ?? "default",
                                ImagesJson = JsonSerializer.Serialize(t.Images ?? new List<string>()),
                                VideoUrl = t.VideoUrl, Notes = t.Notes,
                                IsDefault = true, CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                    if (mapData.Tricks.Camp != null)
                    {
                        foreach (var t in mapData.Tricks.Camp)
                        {
                            db.Tricks.Add(new TrickEntity
                            {
                                ProfileId = profile.Id, MapId = mapId,
                                Name = t.Name, Type = "camp",
                                Side = t.Side ?? "Both", X = t.X, Y = t.Y,
                                Floor = t.Floor ?? "default",
                                ImagesJson = JsonSerializer.Serialize(t.Images ?? new List<string>()),
                                VideoUrl = t.VideoUrl, Notes = t.Notes,
                                IsDefault = true, CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                }
            }
        }

        db.SaveChanges();
     }

     private class DefaultConfig
     {
         public int Version { get; set; }
         public Dictionary<string, DefaultMapData>? Maps { get; set; }
     }

     private class DefaultMapData
     {
         public List<DefaultTargetData>? Targets { get; set; }
         public DefaultTricksData? Tricks { get; set; }
     }

     private class DefaultTricksData
     {
         public List<DefaultTrickData>? Wallbang { get; set; }
         public List<DefaultTrickData>? Boost { get; set; }
         public List<DefaultTrickData>? Jump { get; set; }
         public List<DefaultTrickData>? Camp { get; set; }
     }

     private class DefaultTargetData
     {
         public string Name { get; set; } = "";
         public string Type { get; set; } = "smoke";
         public string Side { get; set; } = "T";
         public double X { get; set; }
         public double Y { get; set; }
         public string? Floor { get; set; }
         public string? Image { get; set; }
         public List<DefaultLineupData>? Lineups { get; set; }
     }

     private class DefaultLineupData
     {
         public double X { get; set; }
         public double Y { get; set; }
         public string? Floor { get; set; }
         public List<string>? StandImages { get; set; }
         public List<string>? AimImages { get; set; }
         public string? AimDescription { get; set; }
         public string? ThrowType { get; set; }
         public string? VideoUrl { get; set; }
         public string? Notes { get; set; }
     }

     private class DefaultTrickData
     {
         public string Name { get; set; } = "";
         public string? Side { get; set; }
         public double X { get; set; }
         public double Y { get; set; }
         public string? Floor { get; set; }
         public List<string>? Images { get; set; }
         public string? VideoUrl { get; set; }
         public string? Notes { get; set; }
     }
 }
