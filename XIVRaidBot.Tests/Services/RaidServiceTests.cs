using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using XIVRaidBot.Data;
using XIVRaidBot.Models;
using XIVRaidBot.Services;

namespace XIVRaidBot.Tests.Services;

public class RaidServiceTests
{
    [Fact]
    public async Task CreateRaidAsync_ShouldCreateRaidWithCorrectProperties()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryContext();
        var logger = TestHelpers.CreateMockLogger<RaidService>();
        var compositionService = new Mock<RaidCompositionService>(context, null);
        
        var service = new RaidService(context, logger, compositionService.Object);
        
        var name = "Test Raid";
        var description = "This is a test raid";
        var scheduledTime = DateTime.UtcNow.AddDays(1);
        var location = "Eden's Promise";
        ulong guildId = 123456789012345678;
        ulong channelId = 234567890123456789;
        
        // Act
        var result = await service.CreateRaidAsync(name, description, scheduledTime, location, guildId, channelId);
        
        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(name);
        result.Description.Should().Be(description);
        result.ScheduledTime.Should().Be(scheduledTime);
        result.Location.Should().Be(location);
        result.GuildId.Should().Be(guildId);
        result.ChannelId.Should().Be(channelId);
        
        // Check if the raid was saved to the database
        var savedRaid = await context.Raids.FindAsync(result.Id);
        savedRaid.Should().NotBeNull();
        savedRaid!.Name.Should().Be(name);
    }
    
    [Fact]
    public async Task GetActiveRaidsAsync_ShouldReturnOnlyFutureRaids()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryContext();
        var logger = TestHelpers.CreateMockLogger<RaidService>();
        var compositionService = new Mock<RaidCompositionService>(context, null);
        
        // Create some test raids - past and future
        var pastRaid = new Raid
        {
            Name = "Past Raid",
            Description = "A raid in the past",
            ScheduledTime = DateTime.UtcNow.AddDays(-1),
            Location = "Eden's Promise",
            GuildId = 123456789012345678,
            ChannelId = 234567890123456789,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            CreatedBy = 345678901234567890
        };
        
        var futureRaid1 = new Raid
        {
            Name = "Future Raid 1",
            Description = "A raid in the future",
            ScheduledTime = DateTime.UtcNow.AddDays(1),
            Location = "Eden's Promise",
            GuildId = 123456789012345678,
            ChannelId = 234567890123456789,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = 345678901234567890
        };
        
        var futureRaid2 = new Raid
        {
            Name = "Future Raid 2",
            Description = "Another raid in the future",
            ScheduledTime = DateTime.UtcNow.AddDays(2),
            Location = "Pandaemonium",
            GuildId = 123456789012345678,
            ChannelId = 234567890123456789,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = 345678901234567890
        };
        
        context.Raids.Add(pastRaid);
        context.Raids.Add(futureRaid1);
        context.Raids.Add(futureRaid2);
        await context.SaveChangesAsync();
        
        var service = new RaidService(context, logger, compositionService.Object);
        
        // Act
        var result = await service.GetActiveRaidsAsync();
        
        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(r => r.Name == "Future Raid 1");
        result.Should().Contain(r => r.Name == "Future Raid 2");
        result.Should().NotContain(r => r.Name == "Past Raid");
    }
    
    [Fact]
    public async Task GetRaidAsync_WithValidId_ShouldReturnRaid()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryContext();
        var logger = TestHelpers.CreateMockLogger<RaidService>();
        var compositionService = new Mock<RaidCompositionService>(context, null);
        
        var raid = new Raid
        {
            Name = "Test Raid",
            Description = "A test raid",
            ScheduledTime = DateTime.UtcNow.AddDays(1),
            Location = "Eden's Promise",
            GuildId = 123456789012345678,
            ChannelId = 234567890123456789,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = 345678901234567890
        };
        
        context.Raids.Add(raid);
        await context.SaveChangesAsync();
        
        var service = new RaidService(context, logger, compositionService.Object);
        
        // Act
        var result = await service.GetRaidAsync(raid.Id);
        
        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Raid");
        result.Id.Should().Be(raid.Id);
    }
    
    [Fact]
    public async Task GetRaidAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryContext();
        var logger = TestHelpers.CreateMockLogger<RaidService>();
        var compositionService = new Mock<RaidCompositionService>(context, null);
        
        var service = new RaidService(context, logger, compositionService.Object);
        
        // Act
        var result = await service.GetRaidAsync(999);
        
        // Assert
        result.Should().BeNull();
    }
    
    [Fact]
    public async Task DeleteRaidAsync_WithValidId_ShouldDeleteRaid()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryContext();
        var logger = TestHelpers.CreateMockLogger<RaidService>();
        var compositionService = new Mock<RaidCompositionService>(context, null);
        
        var raid = new Raid
        {
            Name = "Test Raid",
            Description = "A test raid",
            ScheduledTime = DateTime.UtcNow.AddDays(1),
            Location = "Eden's Promise",
            GuildId = 123456789012345678,
            ChannelId = 234567890123456789,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = 345678901234567890
        };
        
        context.Raids.Add(raid);
        await context.SaveChangesAsync();
        
        var service = new RaidService(context, logger, compositionService.Object);
        
        // Act
        var result = await service.DeleteRaidAsync(raid.Id);
        
        // Assert
        result.Should().BeTrue();
        
        // Verify the raid was deleted
        var deletedRaid = await context.Raids.FindAsync(raid.Id);
        deletedRaid.Should().BeNull();
    }
    
    [Fact]
    public async Task DeleteRaidAsync_WithInvalidId_ShouldReturnFalse()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryContext();
        var logger = TestHelpers.CreateMockLogger<RaidService>();
        var compositionService = new Mock<RaidCompositionService>(context, null);
        
        var service = new RaidService(context, logger, compositionService.Object);
        
        // Act
        var result = await service.DeleteRaidAsync(999);
        
        // Assert
        result.Should().BeFalse();
    }
}
