using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using XIVRaidBot.Models;
using XIVRaidBot.Services;

namespace XIVRaidBot.Tests.Services;

public class AttendanceServiceTests
{
    [Fact]
    public async Task UpdateAttendanceStatusAsync_NewAttendee_ShouldAddAttendance()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryContext();
        
        // Create a test raid
        var raid = new Raid
        {
            Name = "Test Raid",
            Description = "A test raid",
            ScheduledTime = System.DateTime.UtcNow.AddDays(1),
            Location = "Eden's Promise",
            GuildId = 123456789012345678,
            ChannelId = 234567890123456789,
            CreatedAt = System.DateTime.UtcNow,
            CreatedBy = 345678901234567890
        };
        
        context.Raids.Add(raid);
        await context.SaveChangesAsync();
        
        var service = new AttendanceService(context);
        
        var userId = 456789012345678901UL;
        var username = "TestUser";
        var status = AttendanceStatus.Confirmed;
        
        // Act
        var result = await service.UpdateAttendanceStatusAsync(raid.Id, userId, username, status);
        
        // Assert
        result.Should().NotBeNull();
        result.RaidId.Should().Be(raid.Id);
        result.UserId.Should().Be(userId);
        result.Username.Should().Be(username);
        result.Status.Should().Be(status);
        
        // Verify the attendance was added to the database
        var attendance = await context.RaidAttendances
            .FirstOrDefaultAsync(a => a.RaidId == raid.Id && a.UserId == userId);
        attendance.Should().NotBeNull();
        attendance!.Status.Should().Be(status);
    }
    
    [Fact]
    public async Task UpdateAttendanceStatusAsync_ExistingAttendee_ShouldUpdateStatus()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryContext();
        
        // Create a test raid
        var raid = new Raid
        {
            Name = "Test Raid",
            Description = "A test raid",
            ScheduledTime = System.DateTime.UtcNow.AddDays(1),
            Location = "Eden's Promise",
            GuildId = 123456789012345678,
            ChannelId = 234567890123456789,
            CreatedAt = System.DateTime.UtcNow,
            CreatedBy = 345678901234567890
        };
        
        context.Raids.Add(raid);
        
        // Add an existing attendance record
        var userId = 456789012345678901UL;
        var username = "TestUser";
        var initialStatus = AttendanceStatus.Tentative;
        
        var attendance = new RaidAttendance
        {
            RaidId = raid.Id,
            UserId = userId,
            Username = username,
            Status = initialStatus
        };
        
        context.RaidAttendances.Add(attendance);
        await context.SaveChangesAsync();
        
        var service = new AttendanceService(context);
        
        // New status to update to
        var newStatus = AttendanceStatus.Confirmed;
        
        // Act
        var result = await service.UpdateAttendanceStatusAsync(raid.Id, userId, username, newStatus);
        
        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(newStatus);
        
        // Verify the attendance was updated in the database
        var updatedAttendance = await context.RaidAttendances
            .FirstOrDefaultAsync(a => a.RaidId == raid.Id && a.UserId == userId);
        updatedAttendance.Should().NotBeNull();
        updatedAttendance!.Status.Should().Be(newStatus);
    }
    
    [Fact]
    public async Task GetAttendanceAsync_WithValidRaidId_ShouldReturnAllAttendees()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryContext();
        
        // Create a test raid
        var raid = new Raid
        {
            Name = "Test Raid",
            Description = "A test raid",
            ScheduledTime = System.DateTime.UtcNow.AddDays(1),
            Location = "Eden's Promise",
            GuildId = 123456789012345678,
            ChannelId = 234567890123456789,
            CreatedAt = System.DateTime.UtcNow,
            CreatedBy = 345678901234567890
        };
        
        context.Raids.Add(raid);
        
        // Add some attendance records
        var attendances = new[]
        {
            new RaidAttendance { RaidId = raid.Id, UserId = 111222333444555666UL, Username = "User1", Status = AttendanceStatus.Confirmed },
            new RaidAttendance { RaidId = raid.Id, UserId = 222333444555666777UL, Username = "User2", Status = AttendanceStatus.Tentative },
            new RaidAttendance { RaidId = raid.Id, UserId = 333444555666777888UL, Username = "User3", Status = AttendanceStatus.Declined }
        };
        
        context.RaidAttendances.AddRange(attendances);
        await context.SaveChangesAsync();
        
        var service = new AttendanceService(context);
        
        // Act
        var result = await service.GetAttendanceAsync(raid.Id);
        
        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().ContainSingle(a => a.Username == "User1" && a.Status == AttendanceStatus.Confirmed);
        result.Should().ContainSingle(a => a.Username == "User2" && a.Status == AttendanceStatus.Tentative);
        result.Should().ContainSingle(a => a.Username == "User3" && a.Status == AttendanceStatus.Declined);
    }
    
    [Fact]
    public async Task GetAttendanceAsync_WithInvalidRaidId_ShouldReturnEmptyList()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryContext();
        var service = new AttendanceService(context);
        
        // Act
        var result = await service.GetAttendanceAsync(999);
        
        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
    
    [Fact]
    public async Task GetAttendanceStatusAsync_ExistingAttendee_ShouldReturnStatus()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryContext();
        
        // Create a test raid
        var raid = new Raid
        {
            Name = "Test Raid",
            Description = "A test raid",
            ScheduledTime = System.DateTime.UtcNow.AddDays(1),
            Location = "Eden's Promise",
            GuildId = 123456789012345678,
            ChannelId = 234567890123456789,
            CreatedAt = System.DateTime.UtcNow,
            CreatedBy = 345678901234567890
        };
        
        context.Raids.Add(raid);
        
        // Add attendance record
        var userId = 456789012345678901UL;
        var status = AttendanceStatus.Confirmed;
        
        var attendance = new RaidAttendance
        {
            RaidId = raid.Id,
            UserId = userId,
            Username = "TestUser",
            Status = status
        };
        
        context.RaidAttendances.Add(attendance);
        await context.SaveChangesAsync();
        
        var service = new AttendanceService(context);
        
        // Act
        var result = await service.GetAttendanceStatusAsync(raid.Id, userId);
        
        // Assert
        result.Should().Be(status);
    }
    
    [Fact]
    public async Task GetAttendanceStatusAsync_NonExistentAttendee_ShouldReturnNone()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryContext();
        
        // Create a test raid
        var raid = new Raid
        {
            Name = "Test Raid",
            Description = "A test raid",
            ScheduledTime = System.DateTime.UtcNow.AddDays(1),
            Location = "Eden's Promise",
            GuildId = 123456789012345678,
            ChannelId = 234567890123456789,
            CreatedAt = System.DateTime.UtcNow,
            CreatedBy = 345678901234567890
        };
        
        context.Raids.Add(raid);
        await context.SaveChangesAsync();
        
        var service = new AttendanceService(context);
        
        // Act
        var result = await service.GetAttendanceStatusAsync(raid.Id, 999999999999999999UL);
        
        // Assert
        result.Should().Be(AttendanceStatus.None);
    }
    
    [Fact]
    public async Task DeleteAttendancesForRaidAsync_ShouldRemoveAllAttendances()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryContext();
        
        // Create a test raid
        var raid = new Raid
        {
            Name = "Test Raid",
            Description = "A test raid",
            ScheduledTime = System.DateTime.UtcNow.AddDays(1),
            Location = "Eden's Promise",
            GuildId = 123456789012345678,
            ChannelId = 234567890123456789,
            CreatedAt = System.DateTime.UtcNow,
            CreatedBy = 345678901234567890
        };
        
        context.Raids.Add(raid);
        
        // Add some attendance records
        var attendances = new[]
        {
            new RaidAttendance { RaidId = raid.Id, UserId = 111222333444555666UL, Username = "User1", Status = AttendanceStatus.Confirmed },
            new RaidAttendance { RaidId = raid.Id, UserId = 222333444555666777UL, Username = "User2", Status = AttendanceStatus.Tentative }
        };
        
        context.RaidAttendances.AddRange(attendances);
        await context.SaveChangesAsync();
        
        var service = new AttendanceService(context);
        
        // Act
        await service.DeleteAttendancesForRaidAsync(raid.Id);
        
        // Assert
        var remainingAttendances = await context.RaidAttendances
            .Where(a => a.RaidId == raid.Id)
            .ToListAsync();
        
        remainingAttendances.Should().BeEmpty();
    }
}
