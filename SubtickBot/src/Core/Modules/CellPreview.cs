using System.Diagnostics;
using System.Text;
using CellEncoding;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Indev2.Encoder;
using Microsoft.EntityFrameworkCore;
using Modding.PublicInterfaces.Cells;
using MongoDB.Driver;
using SubtickBot.Core.ImageGeneration;
using SubtickBot.Data;
using SubtickBot.ExtensionMethods;
using SubtickBot.Services;
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
    private static readonly LevelImageGenerator LevelImageGenerator = new("./Resources/Sprites");
    private readonly DiscordSocketClient _client;
    private readonly MongoDbService _mongoClient;

    public CellPreview(DiscordSocketClient client, MongoDbService mongoClient)
    {
        _client = client;
        _mongoClient = mongoClient;
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
    public async Task Preview(string level, int zoom = 10)
    {
        //defer the response
        await DeferAsync();

        var users = await _mongoClient.Users.FindAsync(u => u.Id == Context.User.Id, options: new FindOptions<User>
        {
            Limit = 1
        });

        var user = users.FirstOrDefault();

        if (users == null)
        {
            //couldn't connect to the database
            await RespondAsync("Failed to connect to database", ephemeral: true);
            return;
        }

        if(user == null)
        {
            user ??= new User
            {
                Id = Context.User.Id
            };

            await _mongoClient.Users.InsertOneAsync(user);
        }

        var timer = new Stopwatch();
        timer.Start();

        DecodeResult result = default;
        long time;
        try
        {
            var task = Task.Run(() => result = LevelEncoder.Decode(level));
            if (!task.Wait(30000))
            {
               task.Dispose();
               await RespondAsync("Level decoding timed out", ephemeral: true);
               return;
            }

            time = timer.ElapsedMilliseconds;
            timer.Stop();

            user.TotalPreviewTime += time;
            user.LevelsPreviewed++;
        } catch (Exception e)
        {
            user.ErrorsThrown++;
            await FollowupAsync("Invalid level", ephemeral: true);
            return;
        } 

        if(default(DecodeResult).Equals(result))
        {
            await FollowupAsync("Level decoding timed out", ephemeral: true);
            return;
        }

        var originalSize = result.Size;

        //pad
        if (zoom >= 0)
        {
            result.Cells = result.Cells!.ZoomOnCells(ref result.DragSpots, zoom, ref result.Size).ToArray();
        }

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
            .AddField("Size", $"{originalSize.x}x{originalSize.y}", true)
            .WithFooter($"{time}ms");
        
        await _mongoClient.Users.ReplaceOneAsync(u => u!= null && u.Id == Context.User.Id, user);

        await FollowupWithFileAsync(embed:embedBuilder.Build(), attachment: imageAttachment);
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

    [SlashCommand("stats", "Gives stats about the bot")]
    public async Task Stats()
    {
        var users = (await _mongoClient.Users.FindAsync(u => u.Id == Context.User.Id, options: new FindOptions<User>
        {
            Limit = 1
        }));

        var user = await users.FirstOrDefaultAsync();

        if(user == null)
        {
            user = new User
            {
                Id = Context.User.Id
            };

            await _mongoClient.Users.InsertOneAsync(user);
        }

        var embedBuilder = new EmbedBuilder()
            .WithTitle("Stats")
            .WithColor(Color.Blue)
            .AddField("Levels Previewed", user.LevelsPreviewed.ToString(), true)
            .AddField("Total Preview Time", $"{user.TotalPreviewTime}ms", true)
            .AddField("Errors Thrown", user.ErrorsThrown.ToString(), true);

        await RespondAsync(embed: embedBuilder.Build());
    }
}
