using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using XIVRaidBot.Data;
using XIVRaidBot.Services;
using XIVRaidBot.Modules;

namespace XIVRaidBot;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Discord.NET configurations
                services.AddSingleton<DiscordSocketClient>();
                services.AddSingleton<InteractionService>();
                services.AddSingleton<CommandService>();
                
                // Database context
                services.AddDbContext<RaidBotContext>(options =>
                    options.UseSqlServer(context.Configuration.GetConnectionString("DefaultConnection")));
                
                // Bot services
                services.AddSingleton<DiscordBotService>();
                services.AddScoped<RaidService>();
                services.AddScoped<ReminderService>();
                services.AddScoped<AttendanceService>();
                services.AddScoped<RaidCompositionService>();
            })
            .Build();

        await RunAsync(host);
    }
    
    private static async Task RunAsync(IHost host)
    {
        using IServiceScope scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        
        try
        {
            // Ensure database is created
            var dbContext = services.GetRequiredService<RaidBotContext>();
            await dbContext.Database.EnsureCreatedAsync();
            
            // Start bot service
            var botService = services.GetRequiredService<DiscordBotService>();
            await botService.StartAsync();
            
            // Keep the program running
            await Task.Delay(Timeout.Infinite);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
        }
    }
}
