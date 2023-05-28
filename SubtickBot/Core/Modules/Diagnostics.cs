using Discord.Interactions;
using Discord.WebSocket;

namespace SubtickBot.Modules;

public class Diagnostics : InteractionModuleBase
{
    private readonly DiscordSocketClient _client;

    public Diagnostics(DiscordSocketClient client)
    {
        _client = client;
    }

    [SlashCommand("ping", "Pings the bot")]
    public async Task Ping()
    {
        await RespondAsync("Pong!");
    }

    [SlashCommand("echo", "Echos the message")]
    public async Task Echo(string message)
    {
        await RespondAsync(message);
    }
}