 using Microsoft.EntityFrameworkCore;
 using UtilityMaster.Models;

 namespace UtilityMaster.Data;

 public class AppDbContext : DbContext
 {
     public DbSet<ProfileEntity> Profiles => Set<ProfileEntity>();
     public DbSet<TargetEntity> Targets => Set<TargetEntity>();
     public DbSet<LineupEntity> Lineups => Set<LineupEntity>();
     public DbSet<TrickEntity> Tricks => Set<TrickEntity>();
    public DbSet<AimPointEntity> AimPoints => Set<AimPointEntity>();

     public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

     protected override void OnModelCreating(ModelBuilder modelBuilder)
     {
         modelBuilder.Entity<TargetEntity>(e =>
         {
             e.HasKey(t => t.Id);
             e.HasOne(t => t.Profile).WithMany(p => p.Targets).HasForeignKey(t => t.ProfileId);
             e.HasMany(t => t.Lineups).WithOne(l => l.Target).HasForeignKey(l => l.TargetId);
         });

         modelBuilder.Entity<LineupEntity>(e =>
         {
             e.HasKey(l => l.Id);
         });

        modelBuilder.Entity<TrickEntity>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasOne(t => t.Profile).WithMany(p => p.Tricks).HasForeignKey(t => t.ProfileId);
        });

        modelBuilder.Entity<AimPointEntity>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasOne(a => a.Lineup).WithMany(l => l.AimPoints).HasForeignKey(a => a.LineupId);
        });

        modelBuilder.Entity<ProfileEntity>(e =>
         {
             e.HasKey(p => p.Id);
         });
     }
 }
