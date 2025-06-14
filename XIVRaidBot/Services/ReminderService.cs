using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XIVRaidBot.Data;
using XIVRaidBot.Models;

namespace XIVRaidBot.Services;

public class ReminderService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DiscordSocketClient _client;
    private Timer? _reminderTimer;

    private readonly ILogger<ReminderService> _logger;

    public ReminderService(IServiceProvider serviceProvider, DiscordSocketClient client)
    {
        _serviceProvider = serviceProvider;
        _client = client;
        _logger = serviceProvider.GetRequiredService<ILogger<ReminderService>>();
    }
    
    public Task StartAsync()
    {
        // Check for reminders every 30 minutes
        _reminderTimer = new Timer(async _ => await CheckForRemindersAsync(), null, TimeSpan.Zero, TimeSpan.FromMinutes(30));
        return Task.CompletedTask;
    }
    
    private async Task CheckForRemindersAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<RaidBotContext>();
            var now = DateTime.UtcNow;
            
            // Get raids that are scheduled within the next 24-25 hours and don't already have a reminder sent
            var raids = await context.Raids
                .Where(r => !r.IsArchived &&
                       r.ScheduledTime > now.AddHours(24) &&
                       r.ScheduledTime < now.AddHours(25) &&
                       r.ReminderMessageId == 0)
                .ToListAsync();
                
            foreach (var raid in raids)
            {
                await SendReminderAsync(raid, context);
            }
            
            // Get raids that are scheduled within the next 1-2 hours for last reminder
            var soonRaids = await context.Raids
                .Where(r => !r.IsArchived &&
                       r.ScheduledTime > now.AddHours(1) &&
                       r.ScheduledTime < now.AddHours(2))
                .ToListAsync();
                
            foreach (var raid in soonRaids)
            {
                await SendLastReminderAsync(raid, context);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error checking reminders: {ex.Message}");
        }
    }
    
    private async Task SendReminderAsync(Raid raid, RaidBotContext context)
    {
        try
        {
            var channel = _client.GetChannel(raid.ChannelId) as IMessageChannel;
            if (channel == null) return;
            
            var attendees = await context.RaidAttendances
                .Where(a => a.RaidId == raid.Id && a.Status == AttendanceStatus.Pending)
                .ToListAsync();
                
            if (!attendees.Any()) return;
            
            var mentions = string.Join(" ", attendees.Select(a => $"<@{a.UserId}>"));
            
            var embed = new EmbedBuilder()
                .WithTitle($"? Reminder: {raid.Name}")
                .WithDescription($"The raid is scheduled for tomorrow at {raid.ScheduledTime:t}!\n\nPlease confirm your attendance by reacting to this message or using the attendance command.")
                .WithColor(Color.Gold)
                .WithTimestamp(raid.ScheduledTime)
                .AddField("Raid", raid.Name, true)
                .AddField("Time", $"{raid.ScheduledTime:f}", true)
                .AddField("Pending Responses", attendees.Count, true)
                .WithFooter($"Raid ID: {raid.Id}")
                .Build();
                
            var message = await channel.SendMessageAsync(mentions, embed: embed);
            
            // Add reaction options
            await message.AddReactionAsync(new Emoji("\u2705")); // Confirm
            await message.AddReactionAsync(new Emoji("\u274C")); // Decline
            await message.AddReactionAsync(new Emoji("\uD83E\uDE91")); // Bench
            
            // Save the reminder message ID
            raid.ReminderMessageId = message.Id;
            await context.SaveChangesAsync();
            
            // Set up the reaction handler
            _client.ReactionAdded += async (cachedMessage, channel, reaction) =>
            {
                if (cachedMessage.Id != message.Id) return;
                if (reaction.User.Value.IsBot) return;
                
                var userId = reaction.User.Value.Id;
                var emoji = reaction.Emote.Name;
                
                AttendanceStatus status = emoji switch
                {
                    "\u2705" => AttendanceStatus.Confirmed,
                    "\u274C" => AttendanceStatus.Declined,
                    "\uD83E\uDE91" => AttendanceStatus.BenchRequested,
                    _ => AttendanceStatus.Pending
                };
                
                if (status != AttendanceStatus.Pending)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var attendanceService = scope.ServiceProvider.GetRequiredService<AttendanceService>();
                    await attendanceService.UpdateAttendanceStatusAsync(
                        raid.Id, 
                        userId, 
                        reaction.User.Value.Username, 
                        status);
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending raid reminder for raid {raid.Id}: {ex.Message}");
        }
    }
    
    private async Task SendLastReminderAsync(Raid raid, RaidBotContext context)
    {
        try
        {
            var channel = _client.GetChannel(raid.ChannelId) as IMessageChannel;
            if (channel == null) return;
            
            var embed = new EmbedBuilder()
                .WithTitle($"?? Final Reminder: {raid.Name}")
                .WithDescription($"The raid begins in about an hour at {raid.ScheduledTime:t}!\n\nSee you there!")
                .WithColor(Color.Red)
                .WithTimestamp(raid.ScheduledTime)
                .WithFooter($"Raid ID: {raid.Id}")
                .Build();
                
            await channel.SendMessageAsync(embed: embed);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending last reminder for raid {raid.Id}: {ex.Message}");
        }
    }
}