using Discord;
using Discord.Interactions;
using System;
using System.Threading.Tasks;
using XIVRaidBot.Services;

namespace XIVRaidBot.Modules;

public class BaseModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly RaidService _raidService;
    
    public BaseModule(RaidService raidService)
    {
        _raidService = raidService;
    }
    
    [SlashCommand("ping", "Check if the bot is alive")]
    public async Task PingAsync()
    {
        await RespondAsync("Pong! ??");
    }
    
    [SlashCommand("about", "Show information about the bot")]
    public async Task AboutAsync()
    {
        var embed = new EmbedBuilder()
            .WithTitle("FFXIV Raid Bot")
            .WithDescription("A Discord bot for managing FFXIV Savage raid parties and events.")
            .WithColor(Color.Purple)
            .AddField("Features", 
                "• Raid scheduling and reminders\n" +
                "• Attendance tracking\n" +
                "• Bench management for substitutes\n" +
                "• Job role and party composition tracking")
            .WithFooter("Made with Discord.NET")
            .Build();
            
        await RespondAsync(embed: embed);
    }
    
    [SlashCommand("help", "Show available commands")]
    public async Task HelpAsync()
    {
        var embed = new EmbedBuilder()
            .WithTitle("Available Commands")
            .WithColor(Color.Blue)
            .AddField("Basic Commands", 
                "`/ping` - Check if the bot is alive\n" +
                "`/about` - Show information about the bot\n" +
                "`/help` - Show this help message")
            .AddField("Raid Commands",
                "`/raid create` - Create a new raid event\n" +
                "`/raid list` - List upcoming raids\n" +
                "`/raid show` - Show details of a specific raid\n" +
                "`/raid delete` - Delete a raid event")
            .AddField("Attendance Commands",
                "`/attend` - Confirm attendance for a raid\n" +
                "`/decline` - Decline attendance for a raid\n" +
                "`/bench` - Request to be on the bench\n" +
                "`/attendance list` - List attendance for a raid")
            .AddField("Character & Job Commands",
                "`/character register` - Register your character\n" +
                "`/character list` - List your registered characters\n" +
                "`/job assign` - Assign a job to a raid\n" +
                "`/composition` - Show the current raid composition")
            .Build();
            
        await RespondAsync(embed: embed, ephemeral: true);
    }
}