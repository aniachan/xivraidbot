using Discord;
using Discord.Interactions;
using System;
using System.Threading.Tasks;
using System.Linq;
using XIVRaidBot.Services;

namespace XIVRaidBot.Modules;

[Group("raid", "Commands for managing raid events")]
public class RaidModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly RaidService _raidService;
    private readonly AttendanceService _attendanceService;
    private readonly UserSettingsService _userSettingsService;
    
    public RaidModule(
        RaidService raidService, 
        AttendanceService attendanceService,
        UserSettingsService userSettingsService)
    {
        _raidService = raidService;
        _attendanceService = attendanceService;
        _userSettingsService = userSettingsService;
    }
      [SlashCommand("create", "Create a new raid event")]
    public async Task CreateRaidAsync(
        [Summary("name", "The name of the raid")] string name,
        [Summary("description", "A description of the raid")] string description,
        [Summary("date", "The date of the raid (yyyy-MM-dd)")] string date,
        [Summary("time", "The time of the raid (HH:mm)")] string time,
        [Summary("location", "The raid location or duty name")] string location)
    {
        await DeferAsync();
        
        // Check if user has set their timezone
        if (!await _userSettingsService.HasUserSetTimezoneAsync(Context.User.Id))
        {
            var embed = new EmbedBuilder()
                .WithTitle("Timezone Required")
                .WithDescription("You need to set your timezone before creating a raid. This ensures raid times are correctly converted to UTC for all members.")
                .WithColor(Color.Orange)
                .AddField("How to Set Your Timezone", "Use the `/settings timezone` command followed by your timezone ID.\nFor example: `/settings timezone Europe/Stockholm`")
                .AddField("List Available Timezones", "Use the `/settings timezone-list` command to see all available timezone options.")
                .Build();
                
            await FollowupAsync(embed: embed, ephemeral: true);
            return;
        }
        
        if (!DateTime.TryParse($"{date} {time}", out var scheduledTime))
        {
            await FollowupAsync("Invalid date or time format. Please use yyyy-MM-dd for date and HH:mm for time.", ephemeral: true);
            return;
        }
        
        // Convert the time from the user's timezone to UTC
        scheduledTime = await _userSettingsService.ConvertToUtcAsync(Context.User.Id, scheduledTime);
        
        if (scheduledTime <= DateTime.UtcNow)
        {
            await FollowupAsync("Raid time must be in the future.", ephemeral: true);
            return;
        }
        
        var raid = await _raidService.CreateRaidAsync(
            name,
            description,
            scheduledTime,
            location,
            Context.Guild.Id,
            Context.Channel.Id);
            
        // Automatically add the creator to the raid attendees
        await _attendanceService.UpdateAttendanceStatusAsync(
            raid.Id,
            Context.User.Id,
            Context.User.Username,
            Models.AttendanceStatus.Confirmed);
            
        await _raidService.UpdateRaidMessageAsync(raid.Id);
        
        await FollowupAsync($"Raid '{name}' created successfully! Raid ID: {raid.Id}");
    }
    
    [SlashCommand("list", "List upcoming raids")]
    public async Task ListRaidsAsync()
    {
        await DeferAsync();
        
        var raids = await _raidService.GetUpcomingRaidsAsync(Context.Guild.Id);
        
        if (!raids.Any())
        {
            await FollowupAsync("No upcoming raids scheduled.", ephemeral: true);
            return;
        }
        
        var embed = new EmbedBuilder()
            .WithTitle("Upcoming Raids")
            .WithColor(Color.Blue);
            
        foreach (var raid in raids.Take(10)) // Limit to 10 raids to avoid hitting Discord embed limits
        {
            embed.AddField($"{raid.Name} (ID: {raid.Id})", 
                $"**When:** {raid.ScheduledTime:f}\n" +
                $"**Where:** {raid.Location}\n" +
                $"**Description:** {raid.Description}");
        }
        
        if (raids.Count > 10)
        {
            embed.WithFooter($"Showing 10 of {raids.Count} upcoming raids.");
        }
        
        await FollowupAsync(embed: embed.Build());
    }
    
    [SlashCommand("show", "Show details of a specific raid")]
    public async Task ShowRaidAsync(
        [Summary("id", "The ID of the raid to show")] int raidId)
    {
        await DeferAsync();
        
        var raid = await _raidService.GetRaidAsync(raidId);
        if (raid == null)
        {
            await FollowupAsync("Raid not found. Please check the raid ID and try again.", ephemeral: true);
            return;
        }
        
        var attendees = await _attendanceService.GetAttendanceForRaidAsync(raidId);
        
        var embed = new EmbedBuilder()
            .WithTitle(raid.Name)
            .WithDescription(raid.Description)
            .WithColor(Color.Blue)
            .WithTimestamp(raid.ScheduledTime)
            .AddField("Time", $"{raid.ScheduledTime:f}", true)
            .AddField("Location", raid.Location, true);
            
        // Add attendance lists
        var confirmed = attendees.Where(a => a.Status == Models.AttendanceStatus.Confirmed)
            .Select(a => a.UserName);
        var pending = attendees.Where(a => a.Status == Models.AttendanceStatus.Pending)
            .Select(a => a.UserName);
        var declined = attendees.Where(a => a.Status == Models.AttendanceStatus.Declined)
            .Select(a => a.UserName);
        var bench = attendees.Where(a => a.Status == Models.AttendanceStatus.BenchRequested || 
                                       a.Status == Models.AttendanceStatus.OnBench)
            .Select(a => a.UserName);
            
        if (confirmed.Any())
            embed.AddField("? Confirmed", string.Join(", ", confirmed));
        if (pending.Any())
            embed.AddField("? Pending", string.Join(", ", pending));
        if (declined.Any())
            embed.AddField("? Declined", string.Join(", ", declined));
        if (bench.Any())
            embed.AddField("?? Bench", string.Join(", ", bench));
            
        embed.WithFooter($"Raid ID: {raid.Id}");
        
        await FollowupAsync(embed: embed.Build());
    }
    
    [SlashCommand("delete", "Delete a raid event")]
    [RequireUserPermission(GuildPermission.ManageEvents)]
    public async Task DeleteRaidAsync(
        [Summary("id", "The ID of the raid to delete")] int raidId)
    {
        await DeferAsync();
        
        var raid = await _raidService.GetRaidAsync(raidId);
        if (raid == null)
        {
            await FollowupAsync("Raid not found. Please check the raid ID and try again.", ephemeral: true);
            return;
        }
        
        await _raidService.DeleteRaidAsync(raidId);
        await FollowupAsync($"Raid '{raid.Name}' has been deleted.");
    }
    
    [SlashCommand("archive", "Archive a completed raid")]
    [RequireUserPermission(GuildPermission.ManageEvents)]
    public async Task ArchiveRaidAsync(
        [Summary("id", "The ID of the raid to archive")] int raidId)
    {
        await DeferAsync();
        
        var raid = await _raidService.GetRaidAsync(raidId);
        if (raid == null)
        {
            await FollowupAsync("Raid not found. Please check the raid ID and try again.", ephemeral: true);
            return;
        }
        
        await _raidService.ArchiveRaidAsync(raidId);
        await FollowupAsync($"Raid '{raid.Name}' has been archived.");
    }
}