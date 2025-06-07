using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace XIVRaidBot.Services;

public class DiscordBotService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;
    private readonly CommandService _commandService;
    private readonly IConfiguration _configuration;

    private readonly ILogger<DiscordBotService> _logger;

    public DiscordBotService(
        IServiceProvider serviceProvider,
        DiscordSocketClient client,
        InteractionService interactionService,
        CommandService commandService,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _client = client;
        _interactionService = interactionService;
        _commandService = commandService;
        _configuration = configuration;

        _logger = serviceProvider.GetRequiredService<ILogger<DiscordBotService>>();
    }

    public async Task StartAsync()
    {
        // Configure event handlers
        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        
        // Get the bot token from configuration
        string token = _configuration["DiscordBot:Token"] ?? throw new InvalidOperationException("Discord bot token is not configured");
        
        // Configure interaction service
        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);
        _client.InteractionCreated += async (interaction) =>
        {
            var scope = _serviceProvider.CreateScope();
            var ctx = new SocketInteractionContext(_client, interaction);
            await _interactionService.ExecuteCommandAsync(ctx, scope.ServiceProvider);
        };
        
        // Configure command service
        await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);
        _client.MessageReceived += async (message) =>
        {
            if (message is not SocketUserMessage userMessage || userMessage.Author.IsBot)
                return;
                
            int argPos = 0;
            string prefix = _configuration["DiscordBot:Prefix"] ?? "!";
            
            if (!userMessage.HasStringPrefix(prefix, ref argPos))
                return;
                
            var context = new SocketCommandContext(_client, userMessage);
            await _commandService.ExecuteAsync(context, argPos, _serviceProvider);
        };
        
        // Login and start the bot
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
        
        _logger.LogInformation("Bot is starting...");
    }
    
    private Task LogAsync(LogMessage log)
    {
        if (log.Exception != null)
        {
            _logger.LogError(log.Exception, "Discord log error: {Message}", log.Message);
        }
        else
        {
            _logger.LogInformation("Discord log: {Message}", log.Message);
        }
        return Task.CompletedTask;
    }
    
    private async Task ReadyAsync()
    {
        _logger.LogInformation($"Bot is ready and connected as {_client.CurrentUser.Username}");

        // Register slash commands
        try
        {
            #if DEBUG
            // Register commands to a specific guild during development
            var guildId = _configuration["DiscordBot:DevGuildId"];
            if (ulong.TryParse(guildId, out var id))
            {
                await _interactionService.RegisterCommandsToGuildAsync(id);
                _logger.LogInformation($"Registered commands to guild {id}");
            }
            else
            {
                await _interactionService.RegisterCommandsGloballyAsync();
                _logger.LogInformation("Registered commands globally");
            }
            #else
            await _interactionService.RegisterCommandsGloballyAsync();
            _logger.LogInformation("Registered commands globally");
            #endif
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error registering commands: {ex.Message}");
        }
        
        // Start background services
        var reminderService = _serviceProvider.GetRequiredService<ReminderService>();
        await reminderService.StartAsync();
    }
}