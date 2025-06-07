using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using XIVRaidBot.Data;
using XIVRaidBot.Services;

namespace XIVRaidBot;

public class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                var env = hostingContext.HostingEnvironment;
                
                // Clear any existing configuration sources
                config.Sources.Clear();
                
                // Add configuration sources in order of precedence (lowest to highest)
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                string localSettingsPath = Path.Combine(env.ContentRootPath, "appsettings.local.json");
                if (File.Exists(localSettingsPath))
                {
                    config.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);
                }

                config.AddEnvironmentVariables();
                if (args != null)
                {
                    config.AddCommandLine(args);
                }
            })
            .ConfigureServices((context, services) =>
            {
                // Configure discord client options
                var config = new DiscordSocketConfig()
                {
                    //...
                };

                // Discord.NET configurations
                services.AddSingleton<DiscordSocketClient>();
                services.AddSingleton<InteractionService>();
                services.AddSingleton<CommandService>();
                
                // Database context
                services.AddDbContextPool<RaidBotContext>(opt =>
                    opt.UseNpgsql(context.Configuration.GetConnectionString("DefaultConnection")));

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
            // Get the database context
            var dbContext = services.GetRequiredService<RaidBotContext>();
            
            // Apply any pending migrations
            Console.WriteLine("Applying database migrations...");
            await dbContext.Database.MigrateAsync();
            Console.WriteLine("Database migrations applied successfully.");
            
            // Start bot service
            var botService = services.GetRequiredService<DiscordBotService>();
            await botService.StartAsync();
            
            // Keep the program running
            await Task.Delay(Timeout.Infinite);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
