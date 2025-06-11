using Discord;
using Discord.Interactions;
using Moq;
using System;
using System.Threading.Tasks;
using XIVRaidBot.Modules;
using XIVRaidBot.Services;

namespace XIVRaidBot.Tests.Modules;

public class RaidModuleTests
{
    [Fact]
    public async Task CreateRaidAsync_WhenUserHasNoTimezone_ShouldPromptToSetTimezone()
    {
        // Arrange
        var raidServiceMock = new Mock<RaidService>(null, null, null);
        var attendanceServiceMock = new Mock<AttendanceService>(null);
        var userSettingsServiceMock = new Mock<UserSettingsService>(null);
        
        // Set up the mock to return false for HasUserSetTimezoneAsync
        userSettingsServiceMock
            .Setup(s => s.HasUserSetTimezoneAsync(It.IsAny<ulong>()))
            .ReturnsAsync(false);
        
        var module = new RaidModule(
            raidServiceMock.Object,
            attendanceServiceMock.Object,
            userSettingsServiceMock.Object);
        
        // Mock the context
        var contextMock = new Mock<SocketInteractionContext>();
        var userMock = new Mock<IUser>();
        userMock.Setup(u => u.Id).Returns(123456789012345678UL);
        userMock.Setup(u => u.Username).Returns("TestUser");
        contextMock.Setup(c => c.User).Returns(userMock.Object);
        
        // Set the module's context
        var contextProperty = typeof(InteractionModuleBase<SocketInteractionContext>)
            .GetProperty("Context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        contextProperty?.SetValue(module, contextMock.Object);
        
        // Mock DeferAsync and FollowupAsync methods
        var embedSent = false;
        module.GetType().GetField("DeferAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(module, new Func<Task>(() => Task.CompletedTask));
        
        module.GetType().GetField("FollowupAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(module, new Func<string, bool, Embed, Task>((message, ephemeral, embed) => 
            {
                embedSent = true;
                embed.Should().NotBeNull();
                embed.Title.Should().Be("Timezone Required");
                return Task.CompletedTask;
            }));
        
        // Act
        await module.CreateRaidAsync(
            "Test Raid",
            "A test raid",
            "2025-07-01",
            "20:00",
            "Eden's Promise");
        
        // Assert
        embedSent.Should().BeTrue();
        userSettingsServiceMock.Verify(
            s => s.HasUserSetTimezoneAsync(123456789012345678UL),
            Times.Once);
        
        // Verify the raid service was NOT called since user needs to set timezone first
        raidServiceMock.Verify(
            s => s.CreateRaidAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<DateTime>(), 
                It.IsAny<string>(),
                It.IsAny<ulong>(),
                It.IsAny<ulong>()),
            Times.Never);
    }
    
    [Fact]
    public async Task CreateRaidAsync_WithValidTimezone_ShouldCreateRaid()
    {
        // Arrange
        var raidServiceMock = new Mock<RaidService>(null, null, null);
        var attendanceServiceMock = new Mock<AttendanceService>(null);
        var userSettingsServiceMock = new Mock<UserSettingsService>(null);
        
        var userId = 123456789012345678UL;
        var guildId = 234567890123456789UL;
        var channelId = 345678901234567890UL;
        
        // Set up the mock to return true for HasUserSetTimezoneAsync
        userSettingsServiceMock
            .Setup(s => s.HasUserSetTimezoneAsync(userId))
            .ReturnsAsync(true);
        
        // Set up mocks for time conversion
        var localTime = new DateTime(2025, 7, 1, 20, 0, 0);
        var utcTime = new DateTime(2025, 7, 1, 19, 0, 0); // Assuming user is in UTC+1
        
        userSettingsServiceMock
            .Setup(s => s.ConvertToUtcAsync(userId, It.Is<DateTime>(d => d.Date == localTime.Date && d.Hour == localTime.Hour)))
            .ReturnsAsync(utcTime);
        
        // Set up mock raid creation
        var raid = new Models.Raid
        {
            Id = 1,
            Name = "Test Raid",
            Description = "A test raid",
            ScheduledTime = utcTime,
            Location = "Eden's Promise",
            GuildId = guildId,
            ChannelId = channelId
        };
        
        raidServiceMock
            .Setup(s => s.CreateRaidAsync(
                "Test Raid", 
                "A test raid", 
                utcTime, 
                "Eden's Promise",
                guildId,
                channelId))
            .ReturnsAsync(raid);
        
        raidServiceMock
            .Setup(s => s.UpdateRaidMessageAsync(raid.Id))
            .ReturnsAsync(true);
        
        var module = new RaidModule(
            raidServiceMock.Object,
            attendanceServiceMock.Object,
            userSettingsServiceMock.Object);
        
        // Mock the context
        var contextMock = new Mock<SocketInteractionContext>();
        var userMock = new Mock<IUser>();
        userMock.Setup(u => u.Id).Returns(userId);
        userMock.Setup(u => u.Username).Returns("TestUser");
        contextMock.Setup(c => c.User).Returns(userMock.Object);
        
        var guildMock = new Mock<IGuild>();
        guildMock.Setup(g => g.Id).Returns(guildId);
        contextMock.Setup(c => c.Guild).Returns(guildMock.Object);
        
        var channelMock = new Mock<IMessageChannel>();
        channelMock.Setup(c => c.Id).Returns(channelId);
        contextMock.Setup(c => c.Channel).Returns(channelMock.Object);
        
        // Set the module's context
        var contextProperty = typeof(InteractionModuleBase<SocketInteractionContext>)
            .GetProperty("Context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        contextProperty?.SetValue(module, contextMock.Object);
        
        // Mock DeferAsync and FollowupAsync methods
        var successMessageSent = false;
        module.GetType().GetField("DeferAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(module, new Func<Task>(() => Task.CompletedTask));
        
        module.GetType().GetField("FollowupAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(module, new Func<string, bool, Embed, Task>((message, ephemeral, embed) => 
            {
                successMessageSent = message.Contains("created successfully");
                return Task.CompletedTask;
            }));
        
        // Act
        await module.CreateRaidAsync(
            "Test Raid",
            "A test raid",
            "2025-07-01",
            "20:00",
            "Eden's Promise");
        
        // Assert
        successMessageSent.Should().BeTrue();
        
        userSettingsServiceMock.Verify(
            s => s.HasUserSetTimezoneAsync(userId),
            Times.Once);
        
        userSettingsServiceMock.Verify(
            s => s.ConvertToUtcAsync(userId, It.Is<DateTime>(d => d.Date == localTime.Date && d.Hour == localTime.Hour)),
            Times.Once);
        
        raidServiceMock.Verify(
            s => s.CreateRaidAsync(
                "Test Raid", 
                "A test raid", 
                utcTime, 
                "Eden's Promise",
                guildId,
                channelId),
            Times.Once);
        
        attendanceServiceMock.Verify(
            s => s.UpdateAttendanceStatusAsync(1, userId, "TestUser", Models.AttendanceStatus.Confirmed),
            Times.Once);
        
        raidServiceMock.Verify(
            s => s.UpdateRaidMessageAsync(1),
            Times.Once);
    }
    
    [Fact]
    public async Task CreateRaidAsync_WithInvalidDateFormat_ShouldShowError()
    {
        // Arrange
        var raidServiceMock = new Mock<RaidService>(null, null, null);
        var attendanceServiceMock = new Mock<AttendanceService>(null);
        var userSettingsServiceMock = new Mock<UserSettingsService>(null);
        
        // Set up the mock to return true for HasUserSetTimezoneAsync
        userSettingsServiceMock
            .Setup(s => s.HasUserSetTimezoneAsync(It.IsAny<ulong>()))
            .ReturnsAsync(true);
        
        var module = new RaidModule(
            raidServiceMock.Object,
            attendanceServiceMock.Object,
            userSettingsServiceMock.Object);
        
        // Mock the context
        var contextMock = new Mock<SocketInteractionContext>();
        var userMock = new Mock<IUser>();
        userMock.Setup(u => u.Id).Returns(123456789012345678UL);
        userMock.Setup(u => u.Username).Returns("TestUser");
        contextMock.Setup(c => c.User).Returns(userMock.Object);
        
        // Set the module's context
        var contextProperty = typeof(InteractionModuleBase<SocketInteractionContext>)
            .GetProperty("Context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        contextProperty?.SetValue(module, contextMock.Object);
        
        // Mock DeferAsync and FollowupAsync methods
        var errorMessageSent = false;
        module.GetType().GetField("DeferAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(module, new Func<Task>(() => Task.CompletedTask));
        
        module.GetType().GetField("FollowupAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(module, new Func<string, bool, Embed, Task>((message, ephemeral, embed) => 
            {
                errorMessageSent = message.Contains("Invalid date or time format");
                return Task.CompletedTask;
            }));
        
        // Act
        await module.CreateRaidAsync(
            "Test Raid",
            "A test raid",
            "invalid-date",
            "20:00",
            "Eden's Promise");
        
        // Assert
        errorMessageSent.Should().BeTrue();
        
        // Verify the raid service was NOT called due to invalid date format
        raidServiceMock.Verify(
            s => s.CreateRaidAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<DateTime>(), 
                It.IsAny<string>(),
                It.IsAny<ulong>(),
                It.IsAny<ulong>()),
            Times.Never);
    }
    
    [Fact]
    public async Task CreateRaidAsync_WithPastDate_ShouldShowError()
    {
        // Arrange
        var raidServiceMock = new Mock<RaidService>(null, null, null);
        var attendanceServiceMock = new Mock<AttendanceService>(null);
        var userSettingsServiceMock = new Mock<UserSettingsService>(null);
        
        var userId = 123456789012345678UL;
        
        // Set up the mock to return true for HasUserSetTimezoneAsync
        userSettingsServiceMock
            .Setup(s => s.HasUserSetTimezoneAsync(userId))
            .ReturnsAsync(true);
        
        // Set up mocks for time conversion to return a past date
        var pastDate = DateTime.UtcNow.AddDays(-1);
        
        userSettingsServiceMock
            .Setup(s => s.ConvertToUtcAsync(userId, It.IsAny<DateTime>()))
            .ReturnsAsync(pastDate);
        
        var module = new RaidModule(
            raidServiceMock.Object,
            attendanceServiceMock.Object,
            userSettingsServiceMock.Object);
        
        // Mock the context
        var contextMock = new Mock<SocketInteractionContext>();
        var userMock = new Mock<IUser>();
        userMock.Setup(u => u.Id).Returns(userId);
        userMock.Setup(u => u.Username).Returns("TestUser");
        contextMock.Setup(c => c.User).Returns(userMock.Object);
        
        // Set the module's context
        var contextProperty = typeof(InteractionModuleBase<SocketInteractionContext>)
            .GetProperty("Context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        contextProperty?.SetValue(module, contextMock.Object);
        
        // Mock DeferAsync and FollowupAsync methods
        var errorMessageSent = false;
        module.GetType().GetField("DeferAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(module, new Func<Task>(() => Task.CompletedTask));
        
        module.GetType().GetField("FollowupAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(module, new Func<string, bool, Embed, Task>((message, ephemeral, embed) => 
            {
                errorMessageSent = message.Contains("Raid time must be in the future");
                return Task.CompletedTask;
            }));
        
        // Act
        await module.CreateRaidAsync(
            "Test Raid",
            "A test raid",
            "2025-01-01", // The date itself doesn't matter as we mock the conversion
            "12:00",
            "Eden's Promise");
        
        // Assert
        errorMessageSent.Should().BeTrue();
        
        // Verify the raid service was NOT called due to past date
        raidServiceMock.Verify(
            s => s.CreateRaidAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<DateTime>(), 
                It.IsAny<string>(),
                It.IsAny<ulong>(),
                It.IsAny<ulong>()),
            Times.Never);
    }
}
