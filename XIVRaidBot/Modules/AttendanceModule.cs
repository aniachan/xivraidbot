using Discord;
using Discord.Interactions;
using System;
using System.Linq;
using System.Threading.Tasks;
using XIVRaidBot.Models;
using XIVRaidBot.Services;

namespace XIVRaidBot.Modules;

public class AttendanceModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly AttendanceService _attendanceService;
    private readonly RaidService _raidService;
    
    public AttendanceModule(AttendanceService attendanceService, RaidService raidService)
    {
        _attendanceService = attendanceService;
        _raidService = raidService;
    }
    
    [SlashCommand("attend", "Confirm attendance for a raid")]
    public async Task AttendRaidAsync(
        [Summary("id", "The ID of the raid")] int raidId,
        [Summary("note", "Optional note for your attendance")] string? note = null)
    {
        await DeferAsync(ephemeral: true);
        
        var raid = await _raidService.GetRaidAsync(raidId);
        if (raid == null)
        {
            await FollowupAsync("Raid not found. Please check the raid ID and try again.", ephemeral: true);
            return;
        }
        
        await _attendanceService.UpdateAttendanceStatusAsync(
            raidId,
            Context.User.Id,
            Context.User.Username,
            AttendanceStatus.Confirmed,
            note);
            
        await FollowupAsync($"You have confirmed your attendance for '{raid.Name}'.", ephemeral: true);
    }
    
    [SlashCommand("decline", "Decline attendance for a raid")]
    public async Task DeclineRaidAsync(
        [Summary("id", "The ID of the raid")] int raidId,
        [Summary("reason", "Optional reason for declining")] string? reason = null)
    {
        await DeferAsync(ephemeral: true);
        
        var raid = await _raidService.GetRaidAsync(raidId);
        if (raid == null)
        {
            await FollowupAsync("Raid not found. Please check the raid ID and try again.", ephemeral: true);
            return;
        }
        
        await _attendanceService.UpdateAttendanceStatusAsync(
            raidId,
            Context.User.Id,
            Context.User.Username,
            AttendanceStatus.Declined,
            reason);
            
        await FollowupAsync($"You have declined attendance for '{raid.Name}'.", ephemeral: true);
    }
    
    [SlashCommand("bench", "Request to be on the bench for a raid")]
    public async Task BenchRequestAsync(
        [Summary("id", "The ID of the raid")] int raidId,
        [Summary("note", "Optional note for your bench request")] string? note = null)
    {
        await DeferAsync(ephemeral: true);
        
        var raid = await _raidService.GetRaidAsync(raidId);
        if (raid == null)
        {
            await FollowupAsync("Raid not found. Please check the raid ID and try again.", ephemeral: true);
            return;
        }
        
        await _attendanceService.UpdateAttendanceStatusAsync(
            raidId,
            Context.User.Id,
            Context.User.Username,
            AttendanceStatus.BenchRequested,
            note);
            
        await FollowupAsync($"You have been added to the bench for '{raid.Name}'.", ephemeral: true);
    }
    
    [SlashCommand("move-to-bench", "Move a player to the bench")]
    [RequireUserPermission(GuildPermission.ManageEvents)]
    public async Task MoveToBenchAsync(
        [Summary("id", "The ID of the raid")] int raidId,
        [Summary("user", "The user to move to the bench")] IUser user)
    {
        await DeferAsync();
        
        var raid = await _raidService.GetRaidAsync(raidId);
        if (raid == null)
        {
            await FollowupAsync("Raid not found. Please check the raid ID and try again.", ephemeral: true);
            return;
        }
        
        await _attendanceService.MoveToBenchAsync(raidId, user.Id);
        await FollowupAsync($"{user.Username} has been moved to the bench for '{raid.Name}'.");
    }
    
    [SlashCommand("move-from-bench", "Move a player from bench to confirmed")]
    [RequireUserPermission(GuildPermission.ManageEvents)]
    public async Task MoveFromBenchAsync(
        [Summary("id", "The ID of the raid")] int raidId,
        [Summary("user", "The user to move from bench to confirmed")] IUser user)
    {
        await DeferAsync();
        
        var raid = await _raidService.GetRaidAsync(raidId);
        if (raid == null)
        {
            await FollowupAsync("Raid not found. Please check the raid ID and try again.", ephemeral: true);
            return;
        }
        
        await _attendanceService.MoveFromBenchToConfirmedAsync(raidId, user.Id);
        await FollowupAsync($"{user.Username} has been moved from bench to confirmed for '{raid.Name}'.");
    }
    
    [SlashCommand("attendance", "Show attendance for a raid")]
    public async Task ShowAttendanceAsync(
        [Summary("id", "The ID of the raid")] int raidId)
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
            .WithTitle($"Attendance for {raid.Name}")
            .WithDescription($"Scheduled for {raid.ScheduledTime:f}")
            .WithColor(Color.Blue);
            
        var confirmed = attendees.Where(a => a.Status == AttendanceStatus.Confirmed).ToList();
        var pending = attendees.Where(a => a.Status == AttendanceStatus.Pending).ToList();
        var declined = attendees.Where(a => a.Status == AttendanceStatus.Declined).ToList();
        var bench = attendees.Where(a => a.Status == AttendanceStatus.BenchRequested || 
                                       a.Status == AttendanceStatus.OnBench).ToList();
                                       
        embed.AddField($"? Confirmed ({confirmed.Count})", 
            confirmed.Any() ? string.Join("\n", confirmed.Select(a => a.UserName)) : "None");
            
        embed.AddField($"? Pending ({pending.Count})", 
            pending.Any() ? string.Join("\n", pending.Select(a => a.UserName)) : "None");
            
        embed.AddField($"? Declined ({declined.Count})", 
            declined.Any() ? string.Join("\n", declined.Select(a => a.UserName)) : "None");
            
        embed.AddField($"?? Bench ({bench.Count})", 
            bench.Any() ? string.Join("\n", bench.Select(a => a.UserName)) : "None");
            
        await FollowupAsync(embed: embed.Build());
    }
}