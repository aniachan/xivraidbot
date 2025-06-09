using Discord;
using Discord.Interactions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XIVRaidBot.Services;

namespace XIVRaidBot.Modules;

[Group("settings", "Commands for managing user settings")]
public class UserSettingsModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly UserSettingsService _settingsService;
    
    public UserSettingsModule(UserSettingsService settingsService)
    {
        _settingsService = settingsService;
    }
    
    [SlashCommand("timezone", "Set your timezone for raid scheduling")]
    public async Task SetTimezoneAsync(
        [Summary("timezone", "Your timezone (e.g., 'Europe/Stockholm', 'America/New_York')")] string timezone)
    {
        await DeferAsync(ephemeral: true);
        
        var success = await _settingsService.SetUserTimezoneAsync(Context.User.Id, timezone);
        
        if (success)
        {
            await FollowupAsync($"Your timezone has been set to `{timezone}`.", ephemeral: true);
        }
        else
        {
            var timeZones = _settingsService.GetAvailableTimezones();
            
            var embedBuilder = new EmbedBuilder()
                .WithTitle("Invalid Timezone")
                .WithDescription("The timezone you provided is not valid. Here are some examples of valid timezones:")
                .WithColor(Color.Red);
                
            // Show a sampling of common timezones
            var commonTimezones = new List<string>
            {
                "Europe/London", 
                "Europe/Paris",
                "Europe/Stockholm",
                "America/New_York",
                "America/Los_Angeles",
                "Asia/Tokyo",
                "Australia/Sydney",
                "Pacific/Auckland"
            }.Where(tz => timeZones.Contains(tz)).ToList();
            
            embedBuilder.AddField("Common Timezones", string.Join("\n", commonTimezones));
            
            await FollowupAsync(embed: embedBuilder.Build(), ephemeral: true);
        }
    }
    
    [SlashCommand("timezone-list", "List available timezones")]
    public async Task ListTimezonesAsync()
    {
        await DeferAsync(ephemeral: true);
        
        var timezones = _settingsService.GetAvailableTimezones();
        
        // Group by region for better display
        var regions = timezones
            .GroupBy(tz => tz.Split('/').FirstOrDefault() ?? "Other")
            .OrderBy(g => g.Key);
            
        var embedBuilder = new EmbedBuilder()
            .WithTitle("Available Timezones")
            .WithDescription("Use `/settings timezone <timezone>` to set your timezone.")
            .WithColor(Color.Blue);
            
        foreach (var region in regions)
        {
            // Limit the size of each field to comply with Discord's limits
            var tzNames = string.Join(", ", region.Select(tz => $"`{tz}`"));
            
            // Split into chunks if too long
            if (tzNames.Length > 1024)
            {
                var chunks = SplitIntoChunks(region.ToList(), 10);
                var chunkCount = 1;
                
                foreach (var chunk in chunks)
                {
                    embedBuilder.AddField($"{region.Key} ({chunkCount}/{chunks.Count})", 
                        string.Join(", ", chunk.Select(tz => $"`{tz}`")));
                    chunkCount++;
                }
            }
            else
            {
                embedBuilder.AddField(region.Key, tzNames);
            }
        }
        
        await FollowupAsync(embed: embedBuilder.Build(), ephemeral: true);
    }
    
    [SlashCommand("show", "Display your current settings")]
    public async Task ShowSettingsAsync()
    {
        await DeferAsync(ephemeral: true);
        
        var userSettings = await _settingsService.GetUserSettingsAsync(Context.User.Id);
        
        var embedBuilder = new EmbedBuilder()
            .WithTitle("Your Settings")
            .WithColor(Color.Blue);
            
        var timezoneValue = string.IsNullOrEmpty(userSettings.TimeZoneId) 
            ? "Not set (use `/settings timezone` to set it)" 
            : $"`{userSettings.TimeZoneId}`";
            
        embedBuilder.AddField("Timezone", timezoneValue);
        embedBuilder.AddField("Last Updated", $"<t:{new DateTimeOffset(userSettings.LastUpdated).ToUnixTimeSeconds()}:F>");
        
        await FollowupAsync(embed: embedBuilder.Build(), ephemeral: true);
    }
    
    private List<List<T>> SplitIntoChunks<T>(List<T> source, int chunkSize)
    {
        var result = new List<List<T>>();
        
        for (int i = 0; i < source.Count; i += chunkSize)
        {
            result.Add(source.Skip(i).Take(chunkSize).ToList());
        }
        
        return result;
    }
}
