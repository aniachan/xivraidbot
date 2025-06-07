using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XIVRaidBot.Models;
using XIVRaidBot.Services;

namespace XIVRaidBot.Modules;

[Group("character", "Commands for managing FFXIV characters")]
public class CharacterModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly RaidCompositionService _compositionService;
    
    public CharacterModule(RaidCompositionService compositionService)
    {
        _compositionService = compositionService;
    }
    
    [SlashCommand("register", "Register your FFXIV character")]
    public async Task RegisterCharacterAsync(
        [Summary("name", "Your character's name")] string name,
        [Summary("world", "Your character's world")] string world,
        [Summary("job", "Your character's preferred job")] JobType preferredJob)
    {
        await DeferAsync(ephemeral: true);
        
        var character = await _compositionService.RegisterCharacterAsync(
            Context.User.Id,
            name,
            world,
            preferredJob);
            
        await FollowupAsync($"Character {name} from {world} registered successfully!", ephemeral: true);
    }
    
    [SlashCommand("update-jobs", "Update your character's jobs")]
    public async Task UpdateJobsAsync(
        [Summary("character_id", "The ID of your character")] int characterId,
        [Summary("preferred_job", "Your character's preferred job")] JobType preferredJob,
        [Summary("secondary_jobs", "Comma-separated list of secondary jobs")] string? secondaryJobsStr = null)
    {
        await DeferAsync(ephemeral: true);
        
        var character = await _compositionService.GetCharacterAsync(characterId);
        if (character == null || character.UserId != Context.User.Id)
        {
            await FollowupAsync("Character not found or you don't own this character.", ephemeral: true);
            return;
        }
        
        List<JobType>? secondaryJobs = null;
        
        if (!string.IsNullOrWhiteSpace(secondaryJobsStr))
        {
            secondaryJobs = new List<JobType>();
            
            foreach (var jobStr in secondaryJobsStr.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (Enum.TryParse<JobType>(jobStr.Trim(), true, out var job))
                {
                    secondaryJobs.Add(job);
                }
            }
        }
        
        await _compositionService.RegisterCharacterAsync(
            Context.User.Id,
            character.CharacterName,
            character.World,
            preferredJob,
            secondaryJobs);
            
        await FollowupAsync($"Character {character.CharacterName}'s jobs updated successfully!", ephemeral: true);
    }
    
    [SlashCommand("list", "List your registered characters")]
    public async Task ListCharactersAsync()
    {
        await DeferAsync(ephemeral: true);
        
        var characters = await _compositionService.GetUserCharactersAsync(Context.User.Id);
        
        if (!characters.Any())
        {
            await FollowupAsync("You don't have any registered characters. Use `/character register` to add one.", ephemeral: true);
            return;
        }
        
        var embed = new EmbedBuilder()
            .WithTitle("Your Registered Characters")
            .WithColor(Color.Green);
            
        foreach (var character in characters)
        {
            var secondaryJobs = character.SecondaryJobs.Any()
                ? string.Join(", ", character.SecondaryJobs)
                : "None";
                
            embed.AddField($"{character.CharacterName} (ID: {character.Id})",
                $"**World:** {character.World}\n" +
                $"**Preferred Job:** {character.PreferredJob}\n" +
                $"**Secondary Jobs:** {secondaryJobs}");
        }
        
        await FollowupAsync(embed: embed.Build(), ephemeral: true);
    }
}

[Group("job", "Commands for managing raid jobs")]
public class JobModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly RaidCompositionService _compositionService;
    private readonly RaidService _raidService;
    
    public JobModule(RaidCompositionService compositionService, RaidService raidService)
    {
        _compositionService = compositionService;
        _raidService = raidService;
    }
    
    [SlashCommand("assign", "Assign a job for a raid")]
    public async Task AssignJobAsync(
        [Summary("raid_id", "The ID of the raid")] int raidId,
        [Summary("character_id", "The ID of your character")] int characterId,
        [Summary("job", "The job to play in the raid")] JobType job,
        [Summary("role", "Specific role (e.g., MT, OT, etc.)")] string? role = null)
    {
        await DeferAsync(ephemeral: true);
        
        var raid = await _raidService.GetRaidAsync(raidId);
        if (raid == null)
        {
            await FollowupAsync("Raid not found. Please check the raid ID and try again.", ephemeral: true);
            return;
        }
        
        var character = await _compositionService.GetCharacterAsync(characterId);
        if (character == null || character.UserId != Context.User.Id)
        {
            await FollowupAsync("Character not found or you don't own this character.", ephemeral: true);
            return;
        }
        
        var result = await _compositionService.AssignJobToRaidAsync(raidId, characterId, job, role);
        if (result == null)
        {
            await FollowupAsync("Failed to assign job to raid.", ephemeral: true);
            return;
        }
        
        await FollowupAsync($"Successfully assigned {job} to {character.CharacterName} for the raid '{raid.Name}'.", ephemeral: true);
    }
    
    [SlashCommand("remove", "Remove a job assignment from a raid")]
    public async Task RemoveJobAssignmentAsync(
        [Summary("raid_id", "The ID of the raid")] int raidId,
        [Summary("character_id", "The ID of your character")] int characterId)
    {
        await DeferAsync(ephemeral: true);
        
        var raid = await _raidService.GetRaidAsync(raidId);
        if (raid == null)
        {
            await FollowupAsync("Raid not found. Please check the raid ID and try again.", ephemeral: true);
            return;
        }
        
        var character = await _compositionService.GetCharacterAsync(characterId);
        if (character == null || character.UserId != Context.User.Id)
        {
            await FollowupAsync("Character not found or you don't own this character.", ephemeral: true);
            return;
        }
        
        var result = await _compositionService.RemoveJobAssignmentAsync(raidId, characterId);
        if (!result)
        {
            await FollowupAsync("Failed to remove job assignment. The character may not be assigned to this raid.", ephemeral: true);
            return;
        }
        
        await FollowupAsync($"Successfully removed {character.CharacterName} from the raid '{raid.Name}'.", ephemeral: true);
    }
}

[Group("composition", "Commands for managing raid composition")]
public class CompositionModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly RaidCompositionService _compositionService;
    private readonly RaidService _raidService;
    
    public CompositionModule(RaidCompositionService compositionService, RaidService raidService)
    {
        _compositionService = compositionService;
        _raidService = raidService;
    }
    
    [SlashCommand("show", "Show the composition for a raid")]
    public async Task ShowCompositionAsync(
        [Summary("raid_id", "The ID of the raid")] int raidId)
    {
        await DeferAsync();
        
        var raid = await _raidService.GetRaidAsync(raidId);
        if (raid == null)
        {
            await FollowupAsync("Raid not found. Please check the raid ID and try again.", ephemeral: true);
            return;
        }
        
        if (!raid.Compositions.Any())
        {
            await FollowupAsync($"No composition set up for raid '{raid.Name}' yet.", ephemeral: true);
            return;
        }
        
        var embed = new EmbedBuilder()
            .WithTitle($"Raid Composition - {raid.Name}")
            .WithDescription($"Scheduled for {raid.ScheduledTime:f}")
            .WithColor(Color.Gold);
            
        // Group by role
        var tanks = raid.Compositions
            .Where(c => IsJobInRole(c.AssignedJob, JobRole.Tank))
            .Select(c => BuildCompositionEntry(c));
            
        var healers = raid.Compositions
            .Where(c => IsJobInRole(c.AssignedJob, JobRole.Healer))
            .Select(c => BuildCompositionEntry(c));
            
        var dps = raid.Compositions
            .Where(c => IsJobInRole(c.AssignedJob, JobRole.DPS))
            .Select(c => BuildCompositionEntry(c));
            
        embed.AddField($"?? Tanks ({tanks.Count()}/2)", tanks.Any() ? string.Join("\n", tanks) : "None", true);
        embed.AddField($"?? Healers ({healers.Count()}/2)", healers.Any() ? string.Join("\n", healers) : "None", true);
        embed.AddField($"?? DPS ({dps.Count()}/4)", dps.Any() ? string.Join("\n", dps) : "None", false);
        
        var isValid = await _compositionService.IsValidPartyComposition(raidId);
        embed.AddField("Status", isValid ? "? Valid composition" : "?? Incomplete composition");
        
        await FollowupAsync(embed: embed.Build());
    }
    
    private string BuildCompositionEntry(RaidComposition comp)
    {
        string role = !string.IsNullOrEmpty(comp.Role) ? $" ({comp.Role})" : "";
        return $"{comp.AssignedJob}{role} - {comp.Character.CharacterName}";
    }
    
    private bool IsJobInRole(JobType jobType, JobRole role)
    {
        return role switch
        {
            JobRole.Tank => jobType == JobType.PLD || jobType == JobType.WAR || 
                           jobType == JobType.DRK || jobType == JobType.GNB,
                           
            JobRole.Healer => jobType == JobType.WHM || jobType == JobType.SCH || 
                             jobType == JobType.AST || jobType == JobType.SGE,
                             
            JobRole.DPS => jobType != JobType.PLD && jobType != JobType.WAR && 
                          jobType != JobType.DRK && jobType != JobType.GNB &&
                          jobType != JobType.WHM && jobType != JobType.SCH && 
                          jobType != JobType.AST && jobType != JobType.SGE,
                          
            _ => false
        };
    }
}