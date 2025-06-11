using FluentAssertions;
using System.Collections.Generic;
using XIVRaidBot.Models;

namespace XIVRaidBot.Tests.Models;

public class CharacterTests
{
    [Fact]
    public void Character_ShouldStoreAndRetrieveProperties()
    {
        // Arrange
        var character = new Character
        {
            Id = 1,
            UserId = 123456789012345678,
            Name = "Test Character",
            ServerName = "Ragnarok",
            PrimaryJob = JobType.Warrior,
            SecondaryJobs = new List<JobType> { JobType.Paladin, JobType.DarkKnight }
        };
        
        // Assert
        character.Id.Should().Be(1);
        character.UserId.Should().Be(123456789012345678);
        character.Name.Should().Be("Test Character");
        character.ServerName.Should().Be("Ragnarok");
        character.PrimaryJob.Should().Be(JobType.Warrior);
        character.SecondaryJobs.Should().BeEquivalentTo(new List<JobType> { JobType.Paladin, JobType.DarkKnight });
    }
    
    [Fact]
    public void Character_PrimaryRoleProperty_ShouldDeriveFromPrimaryJob()
    {
        // Test tank job
        var tankCharacter = new Character { PrimaryJob = JobType.Warrior };
        tankCharacter.PrimaryRole.Should().Be(RoleType.Tank);
        
        // Test healer job
        var healerCharacter = new Character { PrimaryJob = JobType.WhiteMage };
        healerCharacter.PrimaryRole.Should().Be(RoleType.Healer);
        
        // Test melee DPS job
        var meleeDpsCharacter = new Character { PrimaryJob = JobType.Dragoon };
        meleeDpsCharacter.PrimaryRole.Should().Be(RoleType.DPS);
        
        // Test ranged DPS job
        var rangedDpsCharacter = new Character { PrimaryJob = JobType.Bard };
        rangedDpsCharacter.PrimaryRole.Should().Be(RoleType.DPS);
        
        // Test magic DPS job
        var magicDpsCharacter = new Character { PrimaryJob = JobType.BlackMage };
        magicDpsCharacter.PrimaryRole.Should().Be(RoleType.DPS);
    }
    
    [Fact]
    public void Character_ToString_ShouldIncludeNameAndServer()
    {
        // Arrange
        var character = new Character
        {
            Name = "Test Character",
            ServerName = "Ragnarok",
            PrimaryJob = JobType.Warrior
        };
        
        // Act
        var result = character.ToString();
        
        // Assert
        result.Should().Be("Test Character (Ragnarok)");
    }
}
