using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using XIVRaidBot.Services;

namespace XIVRaidBot.Tests.Services;

public class DiscordBotServiceTests
{
    [Fact]
    public async Task StartAsync_WithValidToken_ShouldConfigureBot()
    {
        // Arrange
        var configurationMock = new Mock<IConfiguration>();
        var clientMock = new Mock<DiscordSocketClient>();
        var interactionServiceMock = new Mock<InteractionService>(clientMock.Object);
        var commandServiceMock = new Mock<CommandService>();
        var loggerMock = new Mock<ILogger<DiscordBotService>>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        var reminderServiceMock = new Mock<ReminderService>(null, null, null);
        
        // Set up mocks
        configurationMock.Setup(c => c["DiscordBot:Token"]).Returns("test_token");
        configurationMock.Setup(c => c["DiscordBot:Prefix"]).Returns("!");
        
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(ILogger<DiscordBotService>)))
            .Returns(loggerMock.Object);
            
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(ReminderService)))
            .Returns(reminderServiceMock.Object);
            
        serviceProviderMock
            .Setup(sp => sp.CreateScope())
            .Returns(Mock.Of<IServiceScope>());
        
        // Set up tracking of calls
        var loginAsyncCalled = false;
        var startAsyncCalled = false;
        var addModulesAsyncCalled = false;
        
        clientMock
            .Setup(c => c.LoginAsync(TokenType.Bot, "test_token", It.IsAny<bool>()))
            .Callback(() => loginAsyncCalled = true)
            .Returns(Task.CompletedTask);
            
        clientMock
            .Setup(c => c.StartAsync())
            .Callback(() => startAsyncCalled = true)
            .Returns(Task.CompletedTask);
            
        interactionServiceMock
            .Setup(i => i.AddModulesAsync(It.IsAny<System.Reflection.Assembly>(), It.IsAny<IServiceProvider>()))
            .Callback(() => addModulesAsyncCalled = true)
            .Returns(Task.CompletedTask);
            
        // Create the service
        var service = new DiscordBotService(
            serviceProviderMock.Object,
            clientMock.Object,
            interactionServiceMock.Object,
            commandServiceMock.Object,
            configurationMock.Object);
            
        // Act
        await service.StartAsync();
        
        // Assert
        loginAsyncCalled.Should().BeTrue();
        startAsyncCalled.Should().BeTrue();
        addModulesAsyncCalled.Should().BeTrue();
        
        // Verify event handlers were registered
        clientMock.Verify(c => c.add_Log(It.IsAny<Func<LogMessage, Task>>()), Times.Once);
        clientMock.Verify(c => c.add_Ready(It.IsAny<Func<Task>>()), Times.Once);
        clientMock.Verify(c => c.add_InteractionCreated(It.IsAny<Func<SocketInteraction, Task>>()), Times.Once);
        clientMock.Verify(c => c.add_MessageReceived(It.IsAny<Func<SocketMessage, Task>>()), Times.Once);
    }
    
    [Fact]
    public void StartAsync_WithMissingToken_ShouldThrowException()
    {
        // Arrange
        var configurationMock = new Mock<IConfiguration>();
        var clientMock = new Mock<DiscordSocketClient>();
        var interactionServiceMock = new Mock<InteractionService>(clientMock.Object);
        var commandServiceMock = new Mock<CommandService>();
        var loggerMock = new Mock<ILogger<DiscordBotService>>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        
        // Set up mocks - returning null for the token to simulate a missing token
        configurationMock.Setup(c => c["DiscordBot:Token"]).Returns((string)null);
        
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(ILogger<DiscordBotService>)))
            .Returns(loggerMock.Object);
        
        // Create the service
        var service = new DiscordBotService(
            serviceProviderMock.Object,
            clientMock.Object,
            interactionServiceMock.Object,
            commandServiceMock.Object,
            configurationMock.Object);
            
        // Act & Assert
        Func<Task> act = async () => await service.StartAsync();
        act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Discord bot token is not configured");
    }
    
    [Fact]
    public async Task ReadyAsync_ShouldRegisterCommands()
    {
        // This test requires accessing a private method, which requires reflection
        // We'll use a simpler approach by testing the class behavior indirectly
        
        // Arrange
        var configurationMock = new Mock<IConfiguration>();
        var clientMock = new Mock<DiscordSocketClient>();
        var interactionServiceMock = new Mock<InteractionService>(clientMock.Object);
        var commandServiceMock = new Mock<CommandService>();
        var loggerMock = new Mock<ILogger<DiscordBotService>>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        var reminderServiceMock = new Mock<ReminderService>(null, null, null);
        
        var currentUserMock = new Mock<ISelfUser>();
        currentUserMock.Setup(u => u.Username).Returns("TestBot");
        clientMock.Setup(c => c.CurrentUser).Returns(currentUserMock.Object);
        
        // Set up configuration for dev guild ID
        configurationMock.Setup(c => c["DiscordBot:Token"]).Returns("test_token");
        configurationMock.Setup(c => c["DiscordBot:DevGuildId"]).Returns("123456789012345678");
        
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(ILogger<DiscordBotService>)))
            .Returns(loggerMock.Object);
            
        serviceProviderMock
            .Setup(sp => sp.GetRequiredService(typeof(ReminderService)))
            .Returns(reminderServiceMock.Object);
        
        // Track if commands were registered
        var commandsRegisteredToGuild = false;
        interactionServiceMock
            .Setup(i => i.RegisterCommandsToGuildAsync(It.IsAny<ulong>(), It.IsAny<bool>()))
            .Callback(() => commandsRegisteredToGuild = true)
            .Returns(Task.CompletedTask);
            
        reminderServiceMock
            .Setup(r => r.StartAsync())
            .Returns(Task.CompletedTask);
        
        // Create the service
        var service = new DiscordBotService(
            serviceProviderMock.Object,
            clientMock.Object,
            interactionServiceMock.Object,
            commandServiceMock.Object,
            configurationMock.Object);
            
        // Act
        await service.StartAsync();
        
        // Trigger the Ready event handler manually
        // This is a bit of a hack, but it allows us to test the behavior
        var readyEvent = clientMock.Invocations
            .FirstOrDefault(i => i.Method.Name == "add_Ready")?
            .Arguments[0] as Func<Task>;
            
        if (readyEvent != null)
        {
            await readyEvent();
        }
        
        // Assert
        commandsRegisteredToGuild.Should().BeTrue();
        interactionServiceMock.Verify(
            i => i.RegisterCommandsToGuildAsync(123456789012345678UL, It.IsAny<bool>()), 
            Times.Once);
        reminderServiceMock.Verify(r => r.StartAsync(), Times.Once);
    }
    
    [Fact]
    public async Task LogAsync_ShouldLogMessages()
    {
        // Arrange
        var configurationMock = new Mock<IConfiguration>();
        var clientMock = new Mock<DiscordSocketClient>();
        var interactionServiceMock = new Mock<InteractionService>(clientMock.Object);
        var commandServiceMock = new Mock<CommandService>();
        var loggerMock = new Mock<ILogger<DiscordBotService>>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        
        configurationMock.Setup(c => c["DiscordBot:Token"]).Returns("test_token");
        
        serviceProviderMock
            .Setup(sp => sp.GetService(typeof(ILogger<DiscordBotService>)))
            .Returns(loggerMock.Object);
        
        // Create the service
        var service = new DiscordBotService(
            serviceProviderMock.Object,
            clientMock.Object,
            interactionServiceMock.Object,
            commandServiceMock.Object,
            configurationMock.Object);
            
        // Act - Manually invoke the log event handler (which is private)
        // We'll need to capture the log handler when it's registered
        Func<LogMessage, Task> logHandler = null;
        clientMock
            .Setup(c => c.add_Log(It.IsAny<Func<LogMessage, Task>>()))
            .Callback<Func<LogMessage, Task>>(handler => logHandler = handler);
            
        await service.StartAsync();
        
        // Now use the captured handler to test both error and info logs
        if (logHandler != null)
        {
            var errorLogMessage = new LogMessage(LogSeverity.Error, "TestSource", "Test error message", new Exception("Test exception"));
            await logHandler(errorLogMessage);
            
            var infoLogMessage = new LogMessage(LogSeverity.Info, "TestSource", "Test info message", null);
            await logHandler(infoLogMessage);
        }
        
        // Assert
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Test error message")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
            
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Test info message")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }
}
