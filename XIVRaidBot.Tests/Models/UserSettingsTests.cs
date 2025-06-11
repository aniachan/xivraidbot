using FluentAssertions;
using System;
using XIVRaidBot.Models;

namespace XIVRaidBot.Tests.Models;

public class UserSettingsTests
{
    [Fact]
    public void UserSettings_ShouldStoreAndRetrieveProperties()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var settings = new UserSettings
        {
            Id = 1,
            UserId = 123456789012345678,
            TimeZoneId = "Europe/London",
            LastUpdated = now
        };
        
        // Assert
        settings.Id.Should().Be(1);
        settings.UserId.Should().Be(123456789012345678);
        settings.TimeZoneId.Should().Be("Europe/London");
        settings.LastUpdated.Should().Be(now);
    }
    
    [Fact]
    public void UserSettings_DefaultValues_ShouldBeCorrect()
    {
        // Arrange
        var settings = new UserSettings();
        
        // Assert
        settings.TimeZoneId.Should().BeEmpty();
        settings.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
