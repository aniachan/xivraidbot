using Discord;
using Discord.Interactions;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using XIVRaidBot.Modules;
using XIVRaidBot.Services;

namespace XIVRaidBot.Tests.Modules;

public class UserSettingsModuleTests
{
    [Fact]
    public async Task SetTimezoneAsync_ValidTimezone_ShouldConfirmSuccess()
    {
        // Arrange
        var userSettingsServiceMock = new Mock<UserSettingsService>(null);
        var validTimezone = "Europe/London";
        
        // Setup the mock to return success for a valid timezone
        userSettingsServiceMock
            .Setup(s => s.SetUserTimezoneAsync(It.IsAny<ulong>(), validTimezone))
            .ReturnsAsync(true);
        
        var module = new UserSettingsModule(userSettingsServiceMock.Object);
        
        // Mock the context
        var contextMock = new Mock<SocketInteractionContext>();
        var userMock = new Mock<IUser>();
        userMock.Setup(u => u.Id).Returns(123456789012345678UL);
        contextMock.Setup(c => c.User).Returns(userMock.Object);
        
        // Set the module's context
        var contextProperty = typeof(InteractionModuleBase<SocketInteractionContext>)
            .GetProperty("Context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        contextProperty?.SetValue(module, contextMock.Object);
        
        // Mock DeferAsync and FollowupAsync methods
        var methodCalled = false;
        module.GetType().GetField("DeferAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(module, new Func<bool, Task>(ephemeral => Task.CompletedTask));
        
        module.GetType().GetField("FollowupAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(module, new Func<string, bool, Embed, Task>((message, ephemeral, embed) => 
            {
                methodCalled = true;
                message.Should().Contain(validTimezone);
                return Task.CompletedTask;
            }));
        
        // Act
        await module.SetTimezoneAsync(validTimezone);
        
        // Assert
        methodCalled.Should().BeTrue();
        userSettingsServiceMock.Verify(
            s => s.SetUserTimezoneAsync(123456789012345678UL, validTimezone), 
            Times.Once);
    }
    
    [Fact]
    public async Task SetTimezoneAsync_InvalidTimezone_ShouldShowErrorAndSuggestions()
    {
        // Arrange
        var userSettingsServiceMock = new Mock<UserSettingsService>(null);
        var invalidTimezone = "Invalid/Timezone";
        
        // Setup the mock to return failure for an invalid timezone
        userSettingsServiceMock
            .Setup(s => s.SetUserTimezoneAsync(It.IsAny<ulong>(), invalidTimezone))
            .ReturnsAsync(false);
        
        // Setup the mock to return some sample timezones
        userSettingsServiceMock
            .Setup(s => s.GetAvailableTimezones())
            .Returns(new List<string> { 
                "Europe/London", 
                "Europe/Paris", 
                "America/New_York", 
                "Asia/Tokyo" 
            });
        
        var module = new UserSettingsModule(userSettingsServiceMock.Object);
        
        // Mock the context
        var contextMock = new Mock<SocketInteractionContext>();
        var userMock = new Mock<IUser>();
        userMock.Setup(u => u.Id).Returns(123456789012345678UL);
        contextMock.Setup(c => c.User).Returns(userMock.Object);
        
        // Set the module's context
        var contextProperty = typeof(InteractionModuleBase<SocketInteractionContext>)
            .GetProperty("Context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        contextProperty?.SetValue(module, contextMock.Object);
        
        // Mock DeferAsync and FollowupAsync methods
        var embedSent = false;
        module.GetType().GetField("DeferAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(module, new Func<bool, Task>(ephemeral => Task.CompletedTask));
        
        module.GetType().GetField("FollowupAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(module, new Func<string, bool, Embed, Task>((message, ephemeral, embed) => 
            {
                embedSent = true;
                embed.Should().NotBeNull();
                embed.Title.Should().Be("Invalid Timezone");
                return Task.CompletedTask;
            }));
        
        // Act
        await module.SetTimezoneAsync(invalidTimezone);
        
        // Assert
        embedSent.Should().BeTrue();
        userSettingsServiceMock.Verify(
            s => s.SetUserTimezoneAsync(123456789012345678UL, invalidTimezone), 
            Times.Once);
        userSettingsServiceMock.Verify(
            s => s.GetAvailableTimezones(),
            Times.Once);
    }
    
    [Fact]
    public async Task ShowSettingsAsync_ShouldDisplayUserSettings()
    {
        // Arrange
        var userSettingsServiceMock = new Mock<UserSettingsService>(null);
        var userId = 123456789012345678UL;
        
        // Setup the mock to return user settings
        userSettingsServiceMock
            .Setup(s => s.GetUserSettingsAsync(userId))
            .ReturnsAsync(new Models.UserSettings
            {
                UserId = userId,
                TimeZoneId = "Europe/London",
                LastUpdated = System.DateTime.UtcNow
            });
        
        var module = new UserSettingsModule(userSettingsServiceMock.Object);
        
        // Mock the context
        var contextMock = new Mock<SocketInteractionContext>();
        var userMock = new Mock<IUser>();
        userMock.Setup(u => u.Id).Returns(userId);
        contextMock.Setup(c => c.User).Returns(userMock.Object);
        
        // Set the module's context
        var contextProperty = typeof(InteractionModuleBase<SocketInteractionContext>)
            .GetProperty("Context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        contextProperty?.SetValue(module, contextMock.Object);
        
        // Mock DeferAsync and FollowupAsync methods
        var embedSent = false;
        module.GetType().GetField("DeferAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(module, new Func<bool, Task>(ephemeral => Task.CompletedTask));
        
        module.GetType().GetField("FollowupAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(module, new Func<string, bool, Embed, Task>((message, ephemeral, embed) => 
            {
                embedSent = true;
                embed.Should().NotBeNull();
                embed.Title.Should().Be("Your Settings");
                
                // Check that the embed contains the timezone field
                embed.Fields.Should().Contain(f => 
                    f.Name == "Timezone" && 
                    f.Value.ToString().Contains("Europe/London"));
                
                return Task.CompletedTask;
            }));
        
        // Act
        await module.ShowSettingsAsync();
        
        // Assert
        embedSent.Should().BeTrue();
        userSettingsServiceMock.Verify(
            s => s.GetUserSettingsAsync(userId), 
            Times.Once);
    }
}
