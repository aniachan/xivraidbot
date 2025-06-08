using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
            .ConfigureLogging((hostContext, logging) =>
            {
                logging.ClearProviders();
                logging.AddConfiguration(hostContext.Configuration.GetSection("Logging"));
                logging.AddConsole();
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

                // Logging service
                services.AddSingleton<LoggingService>();
                
                // Bot services - order matters for dependencies
                services.AddSingleton<DiscordBotService>();
                services.AddSingleton<JobIconService>(); // Add our new JobIconService as a singleton
                services.AddScoped<AttendanceService>();
                services.AddScoped<RaidCompositionService>(); // Register before RaidService
                services.AddScoped<RaidService>(); // Depends on RaidCompositionService
                services.AddScoped<ReminderService>();
            })
            .Build();

        await RunAsync(host);
    }
    
    private static async Task RunAsync(IHost host)
    {
        using IServiceScope scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<Program>>();
        
        try
        {
            // Debug DI container to see registered services
            logger.LogInformation("Listing available services in DI container:");
            ListRegisteredServices(host.Services, logger);
            
            // Get the database context
            var dbContext = services.GetRequiredService<RaidBotContext>();
            
            // Apply any pending migrations
            logger.LogInformation("Applying database migrations...");
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully.");
            
            // Start bot service
            try
            {
                var botService = services.GetRequiredService<DiscordBotService>();
                await botService.StartAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start bot service");
                ValidateRequiredDependencies(services, logger);
                throw; // Rethrow to terminate application
            }
            
            // Keep the program running
            await Task.Delay(Timeout.Infinite);
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Error occurred during application startup");
        }
    }
    
    private static void ListRegisteredServices(IServiceProvider serviceProvider, ILogger<Program> logger)
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
                    logger.LogDebug("Service: {ServiceType}, Lifetime: {Lifetime}, Implementation: {Implementation}",
                        service.ServiceType.FullName,
                        service.Lifetime,
                        service.ImplementationType?.FullName ?? service.ImplementationFactory?.ToString() ?? service.ImplementationInstance?.ToString() ?? "Unknown");
                }
            }
        }
        else
        {
            logger.LogWarning("Unable to retrieve registered services from this service provider implementation.");
        }
    }
    
    private static void ValidateRequiredDependencies(IServiceProvider services, ILogger<Program> logger)
    {
        logger.LogInformation("Validating required dependencies...");
        
        ValidateDependency<DiscordSocketClient>(services, "DiscordSocketClient", logger);
        ValidateDependency<InteractionService>(services, "InteractionService", logger);
        ValidateDependency<CommandService>(services, "CommandService", logger);
        ValidateDependency<DiscordSocketConfig>(services, "DiscordSocketConfig", logger);
        ValidateDependency<RaidBotContext>(services, "RaidBotContext", logger);
        ValidateDependency<IConfiguration>(services, "IConfiguration", logger);
        ValidateDependency<JobIconService>(services, "JobIconService", logger);
        ValidateDependency<RaidCompositionService>(services, "RaidCompositionService", logger);
        ValidateDependency<RaidService>(services, "RaidService", logger);
        
        logger.LogInformation("Required dependency validation completed.");
    }
    
    private static void ValidateDependency<T>(IServiceProvider services, string dependencyName, ILogger<Program> logger)
    {
        try
        {
            var service = services.GetRequiredService<T>();
            logger.LogInformation("✅ {DependencyName} successfully resolved", dependencyName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ {DependencyName} failed to resolve", dependencyName);
        }
    }
}
