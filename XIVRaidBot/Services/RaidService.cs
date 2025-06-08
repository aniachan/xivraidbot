using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XIVRaidBot.Data;
using XIVRaidBot.Models;

namespace XIVRaidBot.Services;

public class RaidService
{
    private readonly RaidBotContext _context;
    private readonly DiscordSocketClient _client;
    private readonly JobIconService _jobIconService;
    
    public RaidService(RaidBotContext context, DiscordSocketClient client, JobIconService jobIconService, RaidCompositionService compositionService)
    {
        _context = context;
        _client = client;
        _jobIconService = jobIconService;
        
        // Subscribe to the RaidCompositionChanged event
        compositionService.RaidCompositionChanged += UpdateRaidMessageAsync;
    }
    
    public async Task<Raid> CreateRaidAsync(
        string name, 
        string description, 
        DateTime scheduledTime, 
        string location, 
        ulong guildId, 
        ulong channelId)
    {
        var raid = new Raid
        {
            Name = name,
            Description = description,
            ScheduledTime = scheduledTime,
            Location = location,
            GuildId = guildId,
            ChannelId = channelId,
            IsArchived = false
        };
        
        raid.ScheduledTime = scheduledTime.ToUniversalTime(); // Ensure time is in UTC
        _context.Raids.Add(raid);
        await _context.SaveChangesAsync();
        
        return raid;
    }

    public async Task<Raid?> GetRaidAsync(int raidId)
    {
        return await _context.Raids
            .Include(r => r.Attendees)
            .Include(r => r.Compositions)
                .ThenInclude(c => c.Character)
            .FirstOrDefaultAsync(r => r.Id == raidId);
    }
    
    public async Task<List<Raid>> GetUpcomingRaidsAsync(ulong guildId)
    {
        return await _context.Raids
            .Where(r => r.GuildId == guildId && r.ScheduledTime > DateTime.UtcNow && !r.IsArchived)
            .OrderBy(r => r.ScheduledTime)
            .ToListAsync();
    }
    
    public async Task UpdateRaidMessageAsync(int raidId)
    {
        var raid = await GetRaidAsync(raidId);
        if (raid == null) return;
        
        var channel = _client.GetChannel(raid.ChannelId) as IMessageChannel;
        if (channel == null) return;
        
        var embed = BuildRaidEmbed(raid);
        
        if (raid.MessageId == 0)
        {
            var message = await channel.SendMessageAsync(embed: embed);
            raid.MessageId = message.Id;
            await _context.SaveChangesAsync();
        }
        else
        {
            try
            {
                var message = await channel.GetMessageAsync(raid.MessageId) as IUserMessage;
                if (message != null)
                {
                    await message.ModifyAsync(props => props.Embed = embed);
                }
                else
                {
                    var newMessage = await channel.SendMessageAsync(embed: embed);
                    raid.MessageId = newMessage.Id;
                    await _context.SaveChangesAsync();
                }
            }
            catch
            {
                var newMessage = await channel.SendMessageAsync(embed: embed);
                raid.MessageId = newMessage.Id;
                await _context.SaveChangesAsync();
            }
        }
    }
    
    public async Task DeleteRaidAsync(int raidId)
    {
        var raid = await _context.Raids.FindAsync(raidId);
        if (raid != null)
        {
            _context.Raids.Remove(raid);
            await _context.SaveChangesAsync();
            
            // Try to delete the message if possible
            try
            {
                var channel = _client.GetChannel(raid.ChannelId) as IMessageChannel;
                if (channel != null && raid.MessageId != 0)
                {
                    var message = await channel.GetMessageAsync(raid.MessageId);
                    if (message != null)
                    {
                        await ((IUserMessage)message).DeleteAsync();
                    }
                }
            }
            catch
            {
                // Ignore errors if the message can't be deleted
            }
        }
    }
    
    public async Task ArchiveRaidAsync(int raidId)
    {
        var raid = await _context.Raids.FindAsync(raidId);
        if (raid != null)
        {
            raid.IsArchived = true;
            await _context.SaveChangesAsync();
        }
    }
    
    private Embed BuildRaidEmbed(Raid raid)
    {
        var embed = new EmbedBuilder()
            .WithTitle(raid.Name)
            .WithDescription(raid.Description)
            .WithColor(Color.Blue)
            .WithTimestamp(raid.ScheduledTime)
            .AddField("Time", $"{raid.ScheduledTime:f}", true)
            .AddField("Location", raid.Location, true);
            
        // Add attendance summary
        var confirmed = raid.Attendees.Count(a => a.Status == AttendanceStatus.Confirmed);
        var pending = raid.Attendees.Count(a => a.Status == AttendanceStatus.Pending);
        var declined = raid.Attendees.Count(a => a.Status == AttendanceStatus.Declined);
        var bench = raid.Attendees.Count(a => a.Status == AttendanceStatus.BenchRequested || a.Status == AttendanceStatus.OnBench);
        
        embed.AddField("Attendance", $"?? Confirmed: {confirmed}\n? Pending: {pending}\n?? Declined: {declined}\n?? Bench: {bench}");
        
        // Add raid composition with job icons
        if (raid.Compositions.Any())
        {
            // Group by role for organized display
            var tanks = raid.Compositions.Where(c => GetRoleFromJobType(c.AssignedJob) == JobRole.Tank);
            var healers = raid.Compositions.Where(c => GetRoleFromJobType(c.AssignedJob) == JobRole.Healer);
            var dps = raid.Compositions.Where(c => GetRoleFromJobType(c.AssignedJob) == JobRole.DPS);
            
            // Format tanks with icons
            if (tanks.Any())
            {
                var tankStr = string.Join("\n", tanks.Select(c => 
                    $"[{c.AssignedJob}]({_jobIconService.GetJobIconUrl(c.AssignedJob)}) - {c.Character.CharacterName}"));
                embed.AddField("Tanks", tankStr, true);
            }
            else
            {
                embed.AddField("Tanks", "None assigned", true);
            }
            
            // Format healers with icons
            if (healers.Any())
            {
                var healerStr = string.Join("\n", healers.Select(c => 
                    $"[{c.AssignedJob}]({_jobIconService.GetJobIconUrl(c.AssignedJob)}) - {c.Character.CharacterName}"));
                embed.AddField("Healers", healerStr, true);
            }
            else
            {
                embed.AddField("Healers", "None assigned", true);
            }
            
            // Format DPS with icons
            if (dps.Any())
            {
                var dpsStr = string.Join("\n", dps.Select(c => 
                    $"[{c.AssignedJob}]({_jobIconService.GetJobIconUrl(c.AssignedJob)}) - {c.Character.CharacterName}"));
                embed.AddField("DPS", dpsStr, true);
            }
            else
            {
                embed.AddField("DPS", "None assigned", true);
            }
        }
        
        embed.WithFooter($"Raid ID: {raid.Id}");
        
        return embed.Build();
    }
    
    private JobRole GetRoleFromJobType(JobType jobType)
    {
        return jobType switch
        {
            JobType.PLD or JobType.WAR or JobType.DRK or JobType.GNB => JobRole.Tank,
            JobType.WHM or JobType.SCH or JobType.AST or JobType.SGE => JobRole.Healer,
            _ => JobRole.DPS
        };
    }
}