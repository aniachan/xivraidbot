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
    
    public RaidService(RaidBotContext context, DiscordSocketClient client)
    {
        _context = context;
        _client = client;
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
            .Where(r => r.GuildId == guildId && r.ScheduledTime > DateTime.Now && !r.IsArchived)
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
        
        embed.AddField("Attendance", $"? Confirmed: {confirmed}\n? Pending: {pending}\n? Declined: {declined}\n?? Bench: {bench}");
        
        // Add raid composition
        if (raid.Compositions.Any())
        {
            var tanks = raid.Compositions.Where(c => GetRoleFromJobType(c.AssignedJob) == JobRole.Tank)
                .Select(c => $"{c.AssignedJob} - {c.Character.CharacterName}");
                
            var healers = raid.Compositions.Where(c => GetRoleFromJobType(c.AssignedJob) == JobRole.Healer)
                .Select(c => $"{c.AssignedJob} - {c.Character.CharacterName}");
                
            var dps = raid.Compositions.Where(c => GetRoleFromJobType(c.AssignedJob) == JobRole.DPS)
                .Select(c => $"{c.AssignedJob} - {c.Character.CharacterName}");
                
            embed.AddField("Tanks", tanks.Any() ? string.Join("\n", tanks) : "None assigned", true);
            embed.AddField("Healers", healers.Any() ? string.Join("\n", healers) : "None assigned", true);
            embed.AddField("DPS", dps.Any() ? string.Join("\n", dps) : "None assigned", true);
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