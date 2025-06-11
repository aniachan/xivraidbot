using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using XIVRaidBot.Models;
using XIVRaidBot.Services;

namespace XIVRaidBot.Tests.Services;

public class UserSettingsServiceTests
{
    [Fact]
    public async Task GetUserSettingsAsync_WhenUserExists_ShouldReturnUserSettings()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryContext();
        var existingUserId = 123456789012345678UL;
        var existingSettings = new UserSettings
        {
            UserId = existingUserId,
            TimeZoneId = "Europe/London",
            LastUpdated = DateTime.UtcNow.AddDays(-1)
        };
        await context.UserSettings.AddAsync(existingSettings);
        await context.SaveChangesAsync();
        
        var service = new UserSettingsService(context);
        
        // Act
        var result = await service.GetUserSettingsAsync(existingUserId);
        
        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(existingUserId);
        result.TimeZoneId.Should().Be("Europe/London");
    }
    
    [Fact]
    public async Task GetUserSettingsAsync_WhenUserDoesNotExist_ShouldCreateAndReturnNewSettings()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryContext();
        var nonExistentUserId = 987654321098765432UL;
        var service = new UserSettingsService(context);
        
        // Act
        var result = await service.GetUserSettingsAsync(nonExistentUserId);
        
        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(nonExistentUserId);
        result.TimeZoneId.Should().BeEmpty();
        
        // Verify the settings were saved to the database
        var savedSettings = await context.UserSettings.FirstOrDefaultAsync(us => us.UserId == nonExistentUserId);
        savedSettings.Should().NotBeNull();
        savedSettings!.UserId.Should().Be(nonExistentUserId);
    }
    
    [Fact]
    public async Task SetUserTimezoneAsync_WithValidTimezone_ShouldUpdateUserSettings()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryContext();
        var userId = 123456789012345678UL;
        var initialTimezone = "Europe/London";
        var newTimezone = "America/New_York";
        
        var existingSettings = new UserSettings
        {
            UserId = userId,
            TimeZoneId = initialTimezone,
            LastUpdated = DateTime.UtcNow.AddDays(-1)
        };
        await context.UserSettings.AddAsync(existingSettings);
        await context.SaveChangesAsync();
        
        var service = new UserSettingsService(context);
        
        // Act
        var result = await service.SetUserTimezoneAsync(userId, newTimezone);
        
        // Assert
        result.Should().BeTrue();
        
        // Verify the settings were updated
        var updatedSettings = await context.UserSettings.FirstOrDefaultAsync(us => us.UserId == userId);
        updatedSettings.Should().NotBeNull();
        updatedSettings!.TimeZoneId.Should().Be(newTimezone);
        updatedSettings.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
    
    [Fact]
    public async Task SetUserTimezoneAsync_WithInvalidTimezone_ShouldReturnFalse()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryContext();
        var userId = 123456789012345678UL;
        var invalidTimezone = "Invalid/TimeZone";
        
        var service = new UserSettingsService(context);
        
        // Act
        var result = await service.SetUserTimezoneAsync(userId, invalidTimezone);
        
        // Assert
        result.Should().BeFalse();
    }
    
    [Fact]
    public async Task HasUserSetTimezoneAsync_WhenTimezoneIsSet_ShouldReturnTrue()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryContext();
        var userId = 123456789012345678UL;
        var timezone = "Europe/London";
        
        var existingSettings = new UserSettings
        {
            UserId = userId,
            TimeZoneId = timezone,
            LastUpdated = DateTime.UtcNow
        };
        await context.UserSettings.AddAsync(existingSettings);
        await context.SaveChangesAsync();
        
        var service = new UserSettingsService(context);
        
        // Act
        var result = await service.HasUserSetTimezoneAsync(userId);
        
        // Assert
        result.Should().BeTrue();
    }
    
    [Fact]
    public async Task HasUserSetTimezoneAsync_WhenTimezoneIsNotSet_ShouldReturnFalse()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryContext();
        var userId = 123456789012345678UL;
        
        var existingSettings = new UserSettings
        {
            UserId = userId,
            TimeZoneId = string.Empty,
            LastUpdated = DateTime.UtcNow
        };
        await context.UserSettings.AddAsync(existingSettings);
        await context.SaveChangesAsync();
        
        var service = new UserSettingsService(context);
        
        // Act
        var result = await service.HasUserSetTimezoneAsync(userId);
        
        // Assert
        result.Should().BeFalse();
    }
    
    [Fact]
    public async Task HasUserSetTimezoneAsync_WhenUserDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryContext();
        var nonExistentUserId = 987654321098765432UL;
        var service = new UserSettingsService(context);
        
        // Act
        var result = await service.HasUserSetTimezoneAsync(nonExistentUserId);
        
        // Assert
        result.Should().BeFalse();
    }
    
    [Fact]
    public async Task ConvertToUtcAsync_WithValidTimezone_ShouldConvertTimeCorrectly()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryContext();
        var userId = 123456789012345678UL;
        var timezone = "America/New_York"; // UTC-5 (or UTC-4 during daylight saving)
        
        var existingSettings = new UserSettings
        {
            UserId = userId,
            TimeZoneId = timezone,
            LastUpdated = DateTime.UtcNow
        };
        await context.UserSettings.AddAsync(existingSettings);
        await context.SaveChangesAsync();
        
        var service = new UserSettingsService(context);
        
        // Create a fixed local time in the America/New_York timezone (e.g., noon)
        var localDateTime = new DateTime(2025, 1, 15, 12, 0, 0);
        
        // Act
        var result = await service.ConvertToUtcAsync(userId, localDateTime);
        
        // Assert
        // Since America/New_York is UTC-5 in January (not during daylight saving),
        // noon in New York should be 17:00 UTC
        var expectedUtcTime = new DateTime(2025, 1, 15, 17, 0, 0);
        result.Should().Be(expectedUtcTime);
    }
    
    [Fact]
    public async Task ConvertToUtcAsync_WhenUserHasNoTimezone_ShouldReturnSameTime()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryContext();
        var userId = 123456789012345678UL;
        
        var existingSettings = new UserSettings
        {
            UserId = userId,
            TimeZoneId = string.Empty,
            LastUpdated = DateTime.UtcNow
        };
        await context.UserSettings.AddAsync(existingSettings);
        await context.SaveChangesAsync();
        
        var service = new UserSettingsService(context);
        
        // Create a test datetime
        var testDateTime = new DateTime(2025, 6, 15, 12, 0, 0);
        
        // Act
        var result = await service.ConvertToUtcAsync(userId, testDateTime);
        
        // Assert
        result.Should().Be(testDateTime);
    }
    
    [Fact]
    public void GetAvailableTimezones_ShouldReturnNonEmptyList()
    {
        // Arrange
        var context = TestHelpers.CreateInMemoryContext();
        var service = new UserSettingsService(context);
        
        // Act
        var result = service.GetAvailableTimezones();
        
        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain(tz => tz == "UTC");
        result.Should().Contain(tz => tz == "Europe/London");
        result.Should().Contain(tz => tz == "America/New_York");
    }
}
