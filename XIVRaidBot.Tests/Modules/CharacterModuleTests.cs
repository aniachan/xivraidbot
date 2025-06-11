using Discord;
using Discord.Interactions;
using Moq;
using System.Threading.Tasks;
using XIVRaidBot.Models;
using XIVRaidBot.Modules;
using XIVRaidBot.Services;

namespace XIVRaidBot.Tests.Modules;

public class CharacterModuleTests
{
    [Fact]
    public async Task RegisterCharacterAsync_ShouldRegisterNewCharacter()
    {
        // Arrange
        var compositionServiceMock = new Mock<RaidCompositionService>(null, null);
        
        var characterName = "Test Character";
        var characterWorld = "Ragnarok";
        var preferredJob = JobType.Warrior;
        var userId = 123456789012345678UL;
        
        var character = new Character
        {
            Id = 1,
            UserId = userId,
            Name = characterName,
            ServerName = characterWorld,
            PrimaryJob = preferredJob
        };
        
        compositionServiceMock
            .Setup(s => s.RegisterCharacterAsync(userId, characterName, characterWorld, preferredJob))
            .ReturnsAsync(character);
        
        var module = new CharacterModule(compositionServiceMock.Object);
        
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
        var messageContainsSuccess = false;
        module.GetType().GetField("DeferAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(module, new Func<bool, Task>(ephemeral => Task.CompletedTask));
        
        module.GetType().GetField("FollowupAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(module, new Func<string, bool, Embed, Task>((message, ephemeral, embed) => 
            {
                messageContainsSuccess = message.Contains("registered successfully");
                return Task.CompletedTask;
            }));
        
        // Act
        await module.RegisterCharacterAsync(characterName, characterWorld, preferredJob);
        
        // Assert
        messageContainsSuccess.Should().BeTrue();
        compositionServiceMock.Verify(
            s => s.RegisterCharacterAsync(userId, characterName, characterWorld, preferredJob), 
            Times.Once);
    }
    
    [Fact]
    public async Task UpdateJobsAsync_WithOwnCharacter_ShouldUpdateJobs()
    {
        // Arrange
        var compositionServiceMock = new Mock<RaidCompositionService>(null, null);
        
        var characterId = 1;
        var userId = 123456789012345678UL;
        var preferredJob = JobType.Paladin;
        var secondaryJobsStr = "Warrior,DarkKnight";
        
        var character = new Character
        {
            Id = characterId,
            UserId = userId,
            Name = "Test Character",
            ServerName = "Ragnarok",
            PrimaryJob = JobType.Warrior
        };
        
        var updatedCharacter = new Character
        {
            Id = characterId,
            UserId = userId,
            Name = "Test Character",
            ServerName = "Ragnarok",
            PrimaryJob = preferredJob,
            SecondaryJobs = new List<JobType> { JobType.Warrior, JobType.DarkKnight }
        };
        
        compositionServiceMock
            .Setup(s => s.GetCharacterAsync(characterId))
            .ReturnsAsync(character);
            
        compositionServiceMock
            .Setup(s => s.UpdateCharacterJobsAsync(characterId, preferredJob, It.IsAny<List<JobType>>()))
            .ReturnsAsync(updatedCharacter);
        
        var module = new CharacterModule(compositionServiceMock.Object);
        
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
        var messageContainsSuccess = false;
        module.GetType().GetField("DeferAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(module, new Func<bool, Task>(ephemeral => Task.CompletedTask));
        
        module.GetType().GetField("FollowupAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(module, new Func<string, bool, Embed, Task>((message, ephemeral, embed) => 
            {
                messageContainsSuccess = message.Contains("updated");
                return Task.CompletedTask;
            }));
        
        // Act
        await module.UpdateJobsAsync(characterId, preferredJob, secondaryJobsStr);
        
        // Assert
        messageContainsSuccess.Should().BeTrue();
        compositionServiceMock.Verify(s => s.GetCharacterAsync(characterId), Times.Once);
        compositionServiceMock.Verify(
            s => s.UpdateCharacterJobsAsync(
                characterId, 
                preferredJob, 
                It.Is<List<JobType>>(jobs => 
                    jobs.Count == 2 && 
                    jobs.Contains(JobType.Warrior) && 
                    jobs.Contains(JobType.DarkKnight))), 
            Times.Once);
    }
    
    [Fact]
    public async Task UpdateJobsAsync_WithOtherUsersCharacter_ShouldReturnError()
    {
        // Arrange
        var compositionServiceMock = new Mock<RaidCompositionService>(null, null);
        
        var characterId = 1;
        var userId = 123456789012345678UL;
        var otherUserId = 987654321098765432UL;
        var preferredJob = JobType.Paladin;
        
        var character = new Character
        {
            Id = characterId,
            UserId = otherUserId, // Different user ID
            Name = "Test Character",
            ServerName = "Ragnarok",
            PrimaryJob = JobType.Warrior
        };
        
        compositionServiceMock
            .Setup(s => s.GetCharacterAsync(characterId))
            .ReturnsAsync(character);
        
        var module = new CharacterModule(compositionServiceMock.Object);
        
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
        var messageContainsError = false;
        module.GetType().GetField("DeferAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(module, new Func<bool, Task>(ephemeral => Task.CompletedTask));
        
        module.GetType().GetField("FollowupAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
            .SetValue(module, new Func<string, bool, Embed, Task>((message, ephemeral, embed) => 
            {
                messageContainsError = message.Contains("don't own this character");
                return Task.CompletedTask;
            }));
        
        // Act
        await module.UpdateJobsAsync(characterId, preferredJob);
        
        // Assert
        messageContainsError.Should().BeTrue();
        compositionServiceMock.Verify(s => s.GetCharacterAsync(characterId), Times.Once);
        compositionServiceMock.Verify(
            s => s.UpdateCharacterJobsAsync(
                It.IsAny<int>(), 
                It.IsAny<JobType>(), 
                It.IsAny<List<JobType>>()), 
            Times.Never);
    }
    
    [Fact]
    public async Task ListCharactersAsync_ShouldShowUserCharacters()
    {
        // Arrange
        var compositionServiceMock = new Mock<RaidCompositionService>(null, null);
        
        var userId = 123456789012345678UL;
        var characters = new List<Character>
        {
            new Character
            {
                Id = 1,
                UserId = userId,
                Name = "Character 1",
                ServerName = "Ragnarok",
                PrimaryJob = JobType.Warrior
            },
            new Character
            {
                Id = 2,
                UserId = userId,
                Name = "Character 2",
                ServerName = "Tonberry",
                PrimaryJob = JobType.WhiteMage
            }
        };
        
        compositionServiceMock
            .Setup(s => s.GetCharactersByUserAsync(userId))
            .ReturnsAsync(characters);
        
        var module = new CharacterModule(compositionServiceMock.Object);
        
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
                embedSent = embed != null;
                if (embed != null)
                {
                    embed.Title.Should().Be("Your Characters");
                    embed.Fields.Should().HaveCount(2); // Two characters
                }
                return Task.CompletedTask;
            }));
        
        // Act - assuming there's a ListCharacters method in CharacterModule
        // Call the method via reflection since we might not have direct access
        var listCharactersMethod = typeof(CharacterModule).GetMethod("ListCharactersAsync");
        if (listCharactersMethod != null)
        {
            await (Task)listCharactersMethod.Invoke(module, null);
        }
        
        // Assert
        embedSent.Should().BeTrue();
        compositionServiceMock.Verify(s => s.GetCharactersByUserAsync(userId), Times.Once);
    }
}
