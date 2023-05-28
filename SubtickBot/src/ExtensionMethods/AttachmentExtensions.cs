using Discord;

namespace SubtickBot.ExtensionMethods;

public static class AttachmentExtensions
{
    public static async Task<Stream> GetStreamAsync(this IAttachment attachment)
    {
        var stream = new MemoryStream();
        var link = attachment.Url;

        using var client = new HttpClient();
        await using var response = await client.GetStreamAsync(link);
        await response.CopyToAsync(stream);
        stream.Position = 0;
        return stream;
    }
}