using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
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
                // Configure discord client options from configuration
                var discordConfig = new DiscordSocketConfig
                {
                    GatewayIntents = Discord.GatewayIntents.AllUnprivileged | Discord.GatewayIntents.MessageContent | Discord.GatewayIntents.GuildMembers,
                    AlwaysDownloadUsers = true,
                    MessageCacheSize = 100
                };

                // Discord.NET configurations
                services.AddSingleton(discordConfig);
                services.AddSingleton<DiscordSocketClient>(sp => new DiscordSocketClient(sp.GetRequiredService<DiscordSocketConfig>()));
                services.AddSingleton<InteractionService>(sp => new InteractionService(sp.GetRequiredService<DiscordSocketClient>()));
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
            // Debug DI container to see registered services
            Console.WriteLine("Listing available services in DI container:");
            ListRegisteredServices(host.Services);
            
            // Get the database context
            var dbContext = services.GetRequiredService<RaidBotContext>();
            
            // Apply any pending migrations
            Console.WriteLine("Applying database migrations...");
            await dbContext.Database.MigrateAsync();
            Console.WriteLine("Database migrations applied successfully.");
            
            // Start bot service
            try
            {
                var botService = services.GetRequiredService<DiscordBotService>();
                await botService.StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start bot service: {ex.Message}");
                Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");
                Console.WriteLine(ex.StackTrace);
                ValidateRequiredDependencies(services);
                throw; // Rethrow to terminate application
            }
            
            // Keep the program running
            await Task.Delay(Timeout.Infinite);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
    
    private static void ListRegisteredServices(IServiceProvider serviceProvider)
    {
        Type serviceProviderType = serviceProvider.GetType();
        var callSiteFactory = serviceProviderType.GetProperty("CallSiteFactory", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(serviceProvider);
        
        if (callSiteFactory != null)
        {
            var descriptors = callSiteFactory.GetType().GetField("_descriptors", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(callSiteFactory);
            
            if (descriptors is System.Collections.Generic.IEnumerable<Microsoft.Extensions.DependencyInjection.ServiceDescriptor> services)
            {
                foreach (var service in services)
                {
                    Console.WriteLine($"Service: {service.ServiceType.FullName}, Lifetime: {service.Lifetime}, Implementation: {service.ImplementationType?.FullName ?? service.ImplementationFactory?.ToString() ?? service.ImplementationInstance?.ToString() ?? "Unknown"}");
                }
            }
        }
        else
        {
            Console.WriteLine("Unable to retrieve registered services from this service provider implementation.");
        }
    }
    
    private static void ValidateRequiredDependencies(IServiceProvider services)
    {
        Console.WriteLine("Validating required dependencies...");
        
        ValidateDependency<DiscordSocketClient>(services, "DiscordSocketClient");
        ValidateDependency<InteractionService>(services, "InteractionService");
        ValidateDependency<CommandService>(services, "CommandService");
        ValidateDependency<DiscordSocketConfig>(services, "DiscordSocketConfig");
        ValidateDependency<RaidBotContext>(services, "RaidBotContext");
        ValidateDependency<IConfiguration>(services, "IConfiguration");
        
        Console.WriteLine("Required dependency validation completed.");
    }
    
    private static void ValidateDependency<T>(IServiceProvider services, string dependencyName)
    {
        try
        {
            var service = services.GetRequiredService<T>();
            Console.WriteLine($"✅ {dependencyName} successfully resolved");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ {dependencyName} failed to resolve: {ex.Message}");
        }
    }
}
