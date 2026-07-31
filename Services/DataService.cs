using Microsoft.EntityFrameworkCore;
using UtilityMaster.Data;
using UtilityMaster.Models;

namespace UtilityMaster.Services;

public class DataService : IDataService
{
    private readonly AppDbContext _db;
    private Guid? _activeProfileId;
    private bool _disposed;

    public DataService()
    {
        _db = DatabaseService.CreateContext();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _db.Dispose();
            _disposed = true;
        }
    }

    // ===== Profiles =====

    public ProfileEntity? GetActiveProfile()
    {
        if (_activeProfileId.HasValue)
            return _db.Profiles.Find(_activeProfileId.Value);
        var first = _db.Profiles.FirstOrDefault();
        if (first != null)
            _activeProfileId = first.Id;
        return first;
    }

    public void SetActiveProfile(Guid profileId)
    {
        _activeProfileId = profileId;
    }

    public List<ProfileEntity> GetAllProfiles()
    {
        return _db.Profiles.AsNoTracking().ToList();
    }

    public ProfileEntity CreateProfile(string name)
    {
        var p = new ProfileEntity { Name = name, CreatedAt = DateTime.UtcNow };
        _db.Profiles.Add(p);
        _db.SaveChanges();
        return p;
    }

    public void DeleteProfile(Guid id)
    {
        var p = _db.Profiles.Find(id);
        if (p != null) { _db.Profiles.Remove(p); _db.SaveChanges(); }
    }

    // ===== Targets =====

    public List<TargetEntity> GetTargets(Guid profileId, string mapId, string type, string side, string floor, bool proOnly)
    {
        var query = _db.Targets
            .Include(t => t.Lineups)
            .ThenInclude(l => l.AimPoints)
            .Where(t => t.ProfileId == profileId && t.MapId == mapId && t.Type == type && t.Floor == floor);

        if (side != "Both")
            query = query.Where(t => t.Side == side);

        if (proOnly)
            query = query.Where(t => t.Lineups.Any(l => l.IsPro));

        return query.AsNoTracking().ToList();
    }

    public List<TargetEntity> GetAllTargets(Guid profileId, string mapId, string? typeFilter = null)
    {
        var query = _db.Targets
            .Include(t => t.Lineups)
            .ThenInclude(l => l.AimPoints)
            .Where(t => t.ProfileId == profileId && t.MapId == mapId);

        if (typeFilter != null)
            query = query.Where(t => t.Type == typeFilter);

        return query.AsNoTracking().ToList();
    }

    public TargetEntity? GetTarget(Guid id)
    {
        return _db.Targets
            .Include(t => t.Lineups)
            .ThenInclude(l => l.AimPoints)
            .FirstOrDefault(t => t.Id == id);
    }

    public void AddTarget(TargetEntity target)
    {
        _db.Targets.Add(target);
        _db.SaveChanges();
    }

    public void DeleteTarget(Guid id)
    {
        var t = _db.Targets.Include(x => x.Lineups).FirstOrDefault(x => x.Id == id);
        if (t != null)
        {
            _db.Lineups.RemoveRange(t.Lineups);
            _db.Targets.Remove(t);
            _db.SaveChanges();
        }
    }

    // ===== Lineups =====

    public void AddLineup(LineupEntity lineup)
    {
        _db.Lineups.Add(lineup);
        _db.SaveChanges();
    }

    public void UpdateLineup(LineupEntity lineup)
    {
        var existing = _db.Lineups.Find(lineup.Id);
        if (existing != null)
        {
            existing.X = lineup.X;
            existing.Y = lineup.Y;
            existing.Name = lineup.Name;
            existing.Side = lineup.Side;
            existing.AimDescription = lineup.AimDescription;
            existing.ThrowType = lineup.ThrowType;
            existing.VideoUrl = lineup.VideoUrl;
            existing.Notes = lineup.Notes;
            existing.ImagesJson = lineup.ImagesJson;
            existing.IsPro = lineup.IsPro;
            _db.SaveChanges();
        }
    }

    public void DeleteLineup(Guid id)
    {
        var l = _db.Lineups.Find(id);
        if (l != null) { _db.Lineups.Remove(l); _db.SaveChanges(); }
    }

    // Lineups (tracked)
    public LineupEntity? GetLineup(Guid id)
    {
        return _db.Lineups.Find(id);
    }

    public List<LineupEntity> GetLineupsQuery(Guid targetId)
    {
        return _db.Lineups.Where(x => x.TargetId == targetId).OrderBy(x => x.Sequence).ToList();
    }

    // ===== Tricks =====

    public TrickEntity? GetTrick(Guid id)
    {
        return _db.Tricks.Find(id);
    }

    public List<TrickEntity> GetTricks(Guid profileId, string mapId)
    {
        return _db.Tricks.Where(t => t.ProfileId == profileId && t.MapId == mapId).AsNoTracking().ToList();
    }

    public void AddTrick(TrickEntity trick)
    {
        _db.Tricks.Add(trick);
        _db.SaveChanges();
    }

    public void UpdateTrick(TrickEntity trick)
    {
        var existing = _db.Tricks.Find(trick.Id);
        if (existing != null)
        {
            existing.Name = trick.Name;
            existing.Type = trick.Type;
            existing.Side = trick.Side;
            existing.X = trick.X;
            existing.Y = trick.Y;
            existing.VideoUrl = trick.VideoUrl;
            existing.Notes = trick.Notes;
            existing.ImagesJson = trick.ImagesJson;
            _db.SaveChanges();
        }
    }

    public void DeleteTrick(Guid id)
    {
        var t = _db.Tricks.Find(id);
        if (t != null) { _db.Tricks.Remove(t); _db.SaveChanges(); }
    }

    public void SaveChanges()
    {
        _db.SaveChanges();
    }
}
