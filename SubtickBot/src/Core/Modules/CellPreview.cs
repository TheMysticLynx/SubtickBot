using System.Diagnostics;
using System.Text;
using CellEncoding;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Indev2.Encoder;
using SubtickBot.Core.ImageGeneration;
using SubtickBot.ExtensionMethods;
using Color = Discord.Color;

namespace SubtickBot.Modules;

public enum EncodingType
{
    Basic,
    V3,
    ByteMash
}

public class CellPreview : InteractionModuleBase
{
    private readonly DiscordSocketClient _client;
    private static readonly LevelImageGenerator LevelImageGenerator = new("./Resources/Sprites");

    public CellPreview(DiscordSocketClient client)
    {
        _client = client;
    }

    [MessageCommand("preview text")]
    public async Task PreviewText(IMessage message)
    {
        //call the preview command with the message content
        await Preview(message.Content);
    }

    [MessageCommand("preview file")]
    public async Task PreviewFile(IMessage message)
    {
        //get the first attachment
        var attachment = message.Attachments.FirstOrDefault();

        //if there is no attachment, return
        if (attachment == null)
        {
            await RespondAsync("No attachment found");
            return;
        }

        //download the attachment
        var stream = await attachment.GetStreamAsync();
        //get utf8 encoding
        var encoding = Encoding.UTF8;
        var text = encoding.GetString((stream as MemoryStream)!.ToArray());

        //call the preview command with the string
        await Preview(text);
    }

    [SlashCommand("preview", "Previews a level")]
    public async Task Preview(string level)
    {
        var timer = new Stopwatch();
        timer.Start();

        DecodeResult result;
        try
        {
            result = LevelEncoder.Decode(level);
        } catch (Exception e)
        {
            await RespondAsync("Invalid level", ephemeral: true);
            return;
        }

        var time = timer.ElapsedMilliseconds;
        timer.Stop();

        var image = LevelImageGenerator.GenerateImage(result);
        var stream = new MemoryStream();

        await image.SaveAsPngAsync(stream);

        //attach the image to the response
        var imageAttachment = new FileAttachment(stream, "image.png");

        var embedBuilder = new EmbedBuilder()
            .WithTitle(result.Name)
            .WithDescription(result.Description)
            .WithColor(Color.Blue)
            .WithImageUrl("attachment://image.png")
            .AddField("Cell Count", result.Cells.Length.ToString(), true)
            .AddField("Size", $"{result.Size.x}x{result.Size.y}", true)
            .WithFooter($"{time}ms");



        await RespondWithFileAsync(embed:embedBuilder.Build(), attachment: imageAttachment);
    }

    [SlashCommand("convert", "Converts a level")]
    public async Task Convert(EncodingType type, string level)
    {
        var result = LevelEncoder.Decode(level);

        var newLevel = result.ToLevel();

        var str = type switch
        {
            EncodingType.Basic => new LevelFormatB().EncodeLevel(newLevel),
            EncodingType.V3 => new LegacyFormatV3().EncodeLevel(newLevel),
            EncodingType.ByteMash => new ByteMash().EncodeLevel(newLevel),
        };

        await RespondAsync(str.Item2);
    }
}