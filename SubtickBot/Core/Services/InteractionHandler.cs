using System.Reflection;
using Discord.Interactions;
using Discord.WebSocket;
using Serilog;

namespace SubtickBot.Services;

public class InteractionHandler
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _services;

    public InteractionHandler(DiscordSocketClient client, InteractionService interactionService, IServiceProvider services)
    {
        _client = client;
        _interactionService = interactionService;
        _services = services;
    }

    public async Task InitializeAsync()
    {
        await _interactionService.AddModulesAsync(Assembly.GetExecutingAssembly(), _services);
        _client.InteractionCreated += HandleInteractionAsync;
        _client.Ready += async () =>
        {
            await _interactionService.RegisterCommandsGloballyAsync();
        };
    }

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        try
        {
            var ctx = new InteractionContext(_client, interaction);
            await _interactionService.ExecuteCommandAsync(ctx, _services);
        } catch (Exception e)
        {
            Log.Warning(e, "Failed to handle interaction");
        }
    }
}