using UtilityMaster.Services;

namespace UtilityMaster.Tests;

public class DatabaseServiceTests
{
    [Fact]
    public void CreateContextCreatesSqliteDatabase()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), "UtilityMaster.Tests", Guid.NewGuid().ToString("N"));

        using var db = DatabaseService.CreateContext(dbPath);

        Assert.True(File.Exists(Path.Combine(dbPath, "data.db")));
    }

    [Fact]
    public void InitializeDefaultsCreatesSingleProfileAndIsIdempotent()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), "UtilityMaster.Tests", Guid.NewGuid().ToString("N"));
        using var db = DatabaseService.CreateContext(dbPath);

        DatabaseService.InitializeDefaults(db);
        DatabaseService.InitializeDefaults(db);

        Assert.Single(db.Profiles.ToList());
    }
}
