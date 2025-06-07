using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using XIVRaidBot.Models;

namespace XIVRaidBot.Data;

public class RaidBotContext : DbContext
{
    public RaidBotContext(DbContextOptions<RaidBotContext> options) : base(options)
    {
    }
    
    public DbSet<Raid> Raids { get; set; } = null!;
    public DbSet<RaidAttendance> RaidAttendances { get; set; } = null!;
    public DbSet<Character> Characters { get; set; } = null!;
    public DbSet<RaidComposition> RaidCompositions { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure Raid entity
        modelBuilder.Entity<Raid>()
            .HasMany(r => r.Attendees)
            .WithOne(a => a.Raid)
            .HasForeignKey(a => a.RaidId);
            
        modelBuilder.Entity<Raid>()
            .HasMany(r => r.Compositions)
            .WithOne(c => c.Raid)
            .HasForeignKey(c => c.RaidId);
            
        // Configure RaidAttendance entity
        modelBuilder.Entity<RaidAttendance>()
            .HasIndex(ra => new { ra.RaidId, ra.UserId })
            .IsUnique();
            
        // Configure Character entity
        modelBuilder.Entity<Character>()
            .HasMany(c => c.RaidCompositions)
            .WithOne(rc => rc.Character)
            .HasForeignKey(rc => rc.CharacterId);
            
        // Configure RaidComposition entity
        modelBuilder.Entity<RaidComposition>()
            .HasIndex(rc => new { rc.RaidId, rc.CharacterId })
            .IsUnique();

        // Convert JobType list to string
        modelBuilder.Entity<Character>()
            .Property(c => c.SecondaryJobs)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<JobType>>(v, (JsonSerializerOptions?)null) ?? new List<JobType>())
            .Metadata
            .SetValueComparer(new ValueComparer<List<JobType>>(
                (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c == null ? new List<JobType>() : new List<JobType>(c)
            ));

    }
}