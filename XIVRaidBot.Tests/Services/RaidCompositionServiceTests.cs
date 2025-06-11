using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XIVRaidBot.Models;
using XIVRaidBot.Services;

namespace XIVRaidBot.Tests.Services;

public class RaidCompositionServiceTests
{
    [Fact]
    public async Task AddCharacterToRaidCompositionAsync_ShouldAddCharacter()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryContext();
        var logger = TestHelpers.CreateMockLogger<RaidCompositionService>();
        var jobIconService = new Mock<JobIconService>(null);
        
        // Create test raid and character
        var raid = new Raid
        {
            Id = 1,
            Name = "Test Raid",
            Description = "A test raid",
            ScheduledTime = System.DateTime.UtcNow.AddDays(1),
            Location = "Eden's Promise",
            GuildId = 123456789012345678,
            ChannelId = 234567890123456789
        };
        
        var character = new Character
        {
            Id = 1,
            UserId = 345678901234567890,
            Name = "Test Character",
            ServerName = "Ragnarok",
            PrimaryJob = JobType.Warrior,
            SecondaryJobs = new List<JobType> { JobType.Paladin, JobType.DarkKnight }
        };
        
        context.Raids.Add(raid);
        context.Characters.Add(character);
        await context.SaveChangesAsync();
        
        var service = new RaidCompositionService(context, logger);
        
        // Act
        var result = await service.AddCharacterToRaidCompositionAsync(raid.Id, character.Id, JobType.Warrior);
        
        // Assert
        result.Should().NotBeNull();
        result.RaidId.Should().Be(raid.Id);
        result.CharacterId.Should().Be(character.Id);
        result.Job.Should().Be(JobType.Warrior);
        
        // Verify it was added to the database
        var composition = await context.RaidCompositions.FindAsync(result.Id);
        composition.Should().NotBeNull();
        composition!.RaidId.Should().Be(raid.Id);
        composition.CharacterId.Should().Be(character.Id);
        composition.Job.Should().Be(JobType.Warrior);
    }
    
    [Fact]
    public async Task RemoveCharacterFromRaidCompositionAsync_ShouldRemoveCharacter()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryContext();
        var logger = TestHelpers.CreateMockLogger<RaidCompositionService>();
        
        // Create test raid and character
        var raid = new Raid
        {
            Id = 1,
            Name = "Test Raid",
            Description = "A test raid",
            ScheduledTime = System.DateTime.UtcNow.AddDays(1),
            Location = "Eden's Promise",
            GuildId = 123456789012345678,
            ChannelId = 234567890123456789
        };
        
        var character = new Character
        {
            Id = 1,
            UserId = 345678901234567890,
            Name = "Test Character",
            ServerName = "Ragnarok",
            PrimaryJob = JobType.Warrior
        };
        
        context.Raids.Add(raid);
        context.Characters.Add(character);
        await context.SaveChangesAsync();
        
        var composition = new RaidComposition
        {
            RaidId = raid.Id,
            CharacterId = character.Id,
            Job = JobType.Warrior,
            Position = 1
        };
        
        context.RaidCompositions.Add(composition);
        await context.SaveChangesAsync();
        
        var service = new RaidCompositionService(context, logger);
        
        // Act
        var result = await service.RemoveCharacterFromRaidCompositionAsync(raid.Id, character.Id);
        
        // Assert
        result.Should().BeTrue();
        
        // Verify it was removed from the database
        var compositions = await context.RaidCompositions
            .Where(rc => rc.RaidId == raid.Id && rc.CharacterId == character.Id)
            .ToListAsync();
        
        compositions.Should().BeEmpty();
    }
    
    [Fact]
    public async Task GetRaidCompositionAsync_ShouldReturnAllCompositions()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryContext();
        var logger = TestHelpers.CreateMockLogger<RaidCompositionService>();
        
        // Create test raid and characters
        var raid = new Raid
        {
            Id = 1,
            Name = "Test Raid",
            Description = "A test raid",
            ScheduledTime = System.DateTime.UtcNow.AddDays(1),
            Location = "Eden's Promise",
            GuildId = 123456789012345678,
            ChannelId = 234567890123456789
        };
        
        var character1 = new Character
        {
            Id = 1,
            UserId = 111222333444555666,
            Name = "Tank Character",
            ServerName = "Ragnarok",
            PrimaryJob = JobType.Warrior
        };
        
        var character2 = new Character
        {
            Id = 2,
            UserId = 222333444555666777,
            Name = "Healer Character",
            ServerName = "Tonberry",
            PrimaryJob = JobType.WhiteMage
        };
        
        var character3 = new Character
        {
            Id = 3,
            UserId = 333444555666777888,
            Name = "DPS Character",
            ServerName = "Cactuar",
            PrimaryJob = JobType.Dragoon
        };
        
        context.Raids.Add(raid);
        context.Characters.AddRange(character1, character2, character3);
        await context.SaveChangesAsync();
        
        var compositions = new[]
        {
            new RaidComposition { RaidId = raid.Id, CharacterId = character1.Id, Job = JobType.Warrior, Position = 1 },
            new RaidComposition { RaidId = raid.Id, CharacterId = character2.Id, Job = JobType.WhiteMage, Position = 3 },
            new RaidComposition { RaidId = raid.Id, CharacterId = character3.Id, Job = JobType.Dragoon, Position = 5 }
        };
        
        context.RaidCompositions.AddRange(compositions);
        await context.SaveChangesAsync();
        
        var service = new RaidCompositionService(context, logger);
        
        // Act
        var result = await service.GetRaidCompositionAsync(raid.Id);
        
        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        
        result.Should().Contain(rc => rc.Character.Name == "Tank Character" && rc.Job == JobType.Warrior);
        result.Should().Contain(rc => rc.Character.Name == "Healer Character" && rc.Job == JobType.WhiteMage);
        result.Should().Contain(rc => rc.Character.Name == "DPS Character" && rc.Job == JobType.Dragoon);
    }
    
    [Fact]
    public async Task UpdateCharacterJobAsync_ShouldChangeJob()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryContext();
        var logger = TestHelpers.CreateMockLogger<RaidCompositionService>();
        
        // Create test raid and character
        var raid = new Raid
        {
            Id = 1,
            Name = "Test Raid",
            Description = "A test raid",
            ScheduledTime = System.DateTime.UtcNow.AddDays(1),
            Location = "Eden's Promise",
            GuildId = 123456789012345678,
            ChannelId = 234567890123456789
        };
        
        var character = new Character
        {
            Id = 1,
            UserId = 345678901234567890,
            Name = "Test Character",
            ServerName = "Ragnarok",
            PrimaryJob = JobType.Warrior,
            SecondaryJobs = new List<JobType> { JobType.Paladin, JobType.DarkKnight }
        };
        
        context.Raids.Add(raid);
        context.Characters.Add(character);
        await context.SaveChangesAsync();
        
        var composition = new RaidComposition
        {
            RaidId = raid.Id,
            CharacterId = character.Id,
            Job = JobType.Warrior,
            Position = 1
        };
        
        context.RaidCompositions.Add(composition);
        await context.SaveChangesAsync();
        
        var service = new RaidCompositionService(context, logger);
        
        // Act - change to another tank job
        var result = await service.UpdateCharacterJobAsync(raid.Id, character.Id, JobType.Paladin);
        
        // Assert
        result.Should().NotBeNull();
        result.Job.Should().Be(JobType.Paladin);
        
        // Verify it was updated in the database
        var updatedComposition = await context.RaidCompositions
            .FirstOrDefaultAsync(rc => rc.RaidId == raid.Id && rc.CharacterId == character.Id);
            
        updatedComposition.Should().NotBeNull();
        updatedComposition!.Job.Should().Be(JobType.Paladin);
    }
    
    [Fact]
    public async Task CheckForDuplicateJobsAsync_WithDuplicates_ShouldReturnTrue()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryContext();
        var logger = TestHelpers.CreateMockLogger<RaidCompositionService>();
        
        // Create test raid and characters
        var raid = new Raid
        {
            Id = 1,
            Name = "Test Raid",
            Description = "A test raid",
            ScheduledTime = System.DateTime.UtcNow.AddDays(1),
            Location = "Eden's Promise",
            GuildId = 123456789012345678,
            ChannelId = 234567890123456789
        };
        
        var character1 = new Character { Id = 1, UserId = 111222333444555666, Name = "Character 1", ServerName = "Ragnarok", PrimaryJob = JobType.Warrior };
        var character2 = new Character { Id = 2, UserId = 222333444555666777, Name = "Character 2", ServerName = "Tonberry", PrimaryJob = JobType.Warrior };
        
        context.Raids.Add(raid);
        context.Characters.AddRange(character1, character2);
        await context.SaveChangesAsync();
        
        var compositions = new[]
        {
            new RaidComposition { RaidId = raid.Id, CharacterId = character1.Id, Job = JobType.Warrior, Position = 1 },
            new RaidComposition { RaidId = raid.Id, CharacterId = character2.Id, Job = JobType.Warrior, Position = 2 } // Same job
        };
        
        context.RaidCompositions.AddRange(compositions);
        await context.SaveChangesAsync();
        
        var service = new RaidCompositionService(context, logger);
        
        // Act
        var result = await service.CheckForDuplicateJobsAsync(raid.Id);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Fact]
    public async Task CheckForDuplicateJobsAsync_WithoutDuplicates_ShouldReturnFalse()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryContext();
        var logger = TestHelpers.CreateMockLogger<RaidCompositionService>();
        
        // Create test raid and characters
        var raid = new Raid
        {
            Id = 1,
            Name = "Test Raid",
            Description = "A test raid",
            ScheduledTime = System.DateTime.UtcNow.AddDays(1),
            Location = "Eden's Promise",
            GuildId = 123456789012345678,
            ChannelId = 234567890123456789
        };
        
        var character1 = new Character { Id = 1, UserId = 111222333444555666, Name = "Character 1", ServerName = "Ragnarok", PrimaryJob = JobType.Warrior };
        var character2 = new Character { Id = 2, UserId = 222333444555666777, Name = "Character 2", ServerName = "Tonberry", PrimaryJob = JobType.WhiteMage };
        
        context.Raids.Add(raid);
        context.Characters.AddRange(character1, character2);
        await context.SaveChangesAsync();
        
        var compositions = new[]
        {
            new RaidComposition { RaidId = raid.Id, CharacterId = character1.Id, Job = JobType.Warrior, Position = 1 },
            new RaidComposition { RaidId = raid.Id, CharacterId = character2.Id, Job = JobType.WhiteMage, Position = 3 } // Different job
        };
        
        context.RaidCompositions.AddRange(compositions);
        await context.SaveChangesAsync();
        
        var service = new RaidCompositionService(context, logger);
        
        // Act
        var result = await service.CheckForDuplicateJobsAsync(raid.Id);
        
        // Assert
        result.Should().BeFalse();
    }
}
