using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using XIVRaidBot.Data;
using XIVRaidBot.Models;

namespace XIVRaidBot.Tests;

/// <summary>
/// Helper class to create test fixtures and mock common objects
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Creates an in-memory database context for testing
    /// </summary>
    public static RaidBotContext CreateInMemoryContext(string dbName = null)
    {
        dbName ??= Guid.NewGuid().ToString();
        
        var options = new DbContextOptionsBuilder<RaidBotContext>()
            .UseInMemoryDatabase(dbName)
            .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
            
        var context = new RaidBotContext(options);
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        return context;
    }
    
    /// <summary>
    /// Seeds the database with test data
    /// </summary>
    public static void SeedTestDatabase(RaidBotContext context)
    {
        // Add test characters
        if (!context.Characters.Any())
        {
            var characters = new List<Character>
            {
                new Character 
                { 
                    Id = 1, 
                    UserId = 123456789012345678, 
                    Name = "TestTank", 
                    ServerName = "Ragnarok", 
                    PrimaryJob = JobType.Warrior, 
                    SecondaryJobs = new List<JobType> { JobType.Paladin, JobType.DarkKnight }
                },
                new Character 
                { 
                    Id = 2, 
                    UserId = 223456789012345678, 
                    Name = "TestHealer", 
                    ServerName = "Tonberry", 
                    PrimaryJob = JobType.WhiteMage, 
                    SecondaryJobs = new List<JobType> { JobType.Scholar, JobType.Sage }
                },
                new Character 
                { 
                    Id = 3, 
                    UserId = 323456789012345678, 
                    Name = "TestDPS", 
                    ServerName = "Cactuar", 
                    PrimaryJob = JobType.Dragoon, 
                    SecondaryJobs = new List<JobType> { JobType.Monk, JobType.Ninja }
                }
            };
            
            context.Characters.AddRange(characters);
        }
        
        // Add test raids
        if (!context.Raids.Any())
        {
            var raids = new List<Raid>
            {
                new Raid
                {
                    Id = 1,
                    Name = "Test Raid 1",
                    Description = "A test raid for unit testing",
                    ScheduledTime = DateTime.UtcNow.AddDays(1),
                    Location = "The Epic of Alexander (Ultimate)",
                    GuildId = 111222333444555666,
                    ChannelId = 111222333444555777,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = 123456789012345678
                },
                new Raid
                {
                    Id = 2,
                    Name = "Test Raid 2",
                    Description = "Another test raid for unit testing",
                    ScheduledTime = DateTime.UtcNow.AddDays(2),
                    Location = "Dragonsong's Reprise (Ultimate)",
                    GuildId = 111222333444555666,
                    ChannelId = 111222333444555777,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = 223456789012345678
                }
            };
            
            context.Raids.AddRange(raids);
        }
        
        // Add test user settings
        if (!context.UserSettings.Any())
        {
            var userSettings = new List<UserSettings>
            {
                new UserSettings
                {
                    Id = 1,
                    UserId = 123456789012345678,
                    TimeZoneId = "Europe/London",
                    LastUpdated = DateTime.UtcNow
                },
                new UserSettings
                {
                    Id = 2,
                    UserId = 223456789012345678,
                    TimeZoneId = "America/New_York",
                    LastUpdated = DateTime.UtcNow
                }
            };
            
            context.UserSettings.AddRange(userSettings);
        }
        
        context.SaveChanges();
    }
    
    /// <summary>
    /// Creates a mock logger
    /// </summary>
    public static ILogger<T> CreateMockLogger<T>()
    {
        return Mock.Of<ILogger<T>>();
    }
    
    /// <summary>
    /// Creates a service provider with common services for testing
    /// </summary>
    public static IServiceProvider CreateServiceProvider(RaidBotContext context = null)
    {
        var services = new ServiceCollection();
        
        // Add DbContext
        if (context != null)
        {
            services.AddSingleton(context);
        }
        else
        {
            services.AddSingleton(CreateInMemoryContext());
        }
        
        // Add logging
        services.AddLogging();
        
        return services.BuildServiceProvider();
    }
}
