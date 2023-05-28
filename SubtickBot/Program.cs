using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using SubtickBot.Modules;
using SubtickBot.Services;

public class Program
{
    public static readonly ILogger _logger = new LoggerConfiguration()
        .WriteTo.Console()
        .MinimumLevel.Verbose()
        .CreateLogger();

    public static Task Main() => new Program().MainAsync();

    public async Task MainAsync()
    {
        using IHost host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
                services
                    .AddSingleton(new DiscordSocketConfig
                            {
                                GatewayIntents = GatewayIntents.AllUnprivileged,
                                AlwaysDownloadUsers = true
                            })
                    .AddSingleton<DiscordSocketClient>()
                    .AddSingleton<InteractionService>()
                    .AddSingleton<InteractionHandler>()
                    .AddSingleton<Diagnostics>())
            .Build();

        await RunAsync(host);
    }

    public async Task RunAsync(IHost host)
    {
        using IServiceScope scope = host.Services.CreateScope();
        IServiceProvider services = scope.ServiceProvider;

        DiscordSocketClient client = services.GetRequiredService<DiscordSocketClient>();
        client.Log += LogAsync;

        var interactionService = services.GetRequiredService<InteractionService>();
        interactionService.Log += LogAsync;
        var handler = services.GetRequiredService<InteractionHandler>();
        await handler.InitializeAsync();

        await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_TOKEN"));
        await client.StartAsync();

        await Task.Delay(-1);
    }

    private static async Task LogAsync(LogMessage message)
    {
        var severity = message.Severity switch
        {
            LogSeverity.Critical => LogEventLevel.Fatal,
            LogSeverity.Error => LogEventLevel.Error,
            LogSeverity.Warning => LogEventLevel.Warning,
            LogSeverity.Info => LogEventLevel.Information,
            LogSeverity.Verbose => LogEventLevel.Verbose,
            LogSeverity.Debug => LogEventLevel.Debug,
            _ => LogEventLevel.Information
        };

        _logger.Write(severity, message.Exception, "{Source}: {Message}", message.Source, message.Message);
    }
}