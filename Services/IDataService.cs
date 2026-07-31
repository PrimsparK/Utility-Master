using UtilityMaster.Models;

namespace UtilityMaster.Services;

public interface IDataService : IDisposable
{
    // Profiles
    ProfileEntity? GetActiveProfile();
    void SetActiveProfile(Guid profileId);
    List<ProfileEntity> GetAllProfiles();
    ProfileEntity CreateProfile(string name);
    void DeleteProfile(Guid id);

    // Targets (nades + wallbang/jump)
    List<TargetEntity> GetTargets(Guid profileId, string mapId, string type, string side, string floor, bool proOnly);
    List<TargetEntity> GetAllTargets(Guid profileId, string mapId, string? typeFilter = null);
    TargetEntity? GetTarget(Guid id);
    void AddTarget(TargetEntity target);
        void DeleteTarget(Guid id);

    // Lineups
    void AddLineup(LineupEntity lineup);
    void UpdateLineup(LineupEntity lineup);
    void DeleteLineup(Guid id);
    
    // Tricks (boost/camp standalone, wallbang/jump via Target+Lineup)
    // Tricks (boost/camp standalone, wallbang/jump via Target+Lineup)
    TrickEntity? GetTrick(Guid id);
    List<TrickEntity> GetTricks(Guid profileId, string mapId);
    void AddTrick(TrickEntity trick);
    void UpdateTrick(TrickEntity trick);
    void DeleteTrick(Guid id);

    // Lineups (tracked)
    LineupEntity? GetLineup(Guid id);
    List<LineupEntity> GetLineupsQuery(Guid targetId);
            
    // Save
    void SaveChanges();
}
