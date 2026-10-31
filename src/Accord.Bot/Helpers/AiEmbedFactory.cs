using System.Drawing;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;

namespace Accord.Bot.Helpers;

[RegisterScoped]
public class AiEmbedFactory(ThumbnailHelper thumbnailHelper)
{
    internal const string ReplyFooterText = "❌ Ownership is load-bearing";

    public Embed CreateProfile(IUser user, string description)
    {
        var avatar = thumbnailHelper.GetAvatar(user);
        return new Embed(
            Author: new EmbedAuthor(user.Username, IconUrl: avatar.Url),
            Thumbnail: avatar,
            Description: description,
            Footer: new EmbedFooter(ReplyFooterText));
    }

    public Embed CreateQuote(IUser user, string quote) => new(
        Colour: Color.Green,
        Author: new EmbedAuthor(user.Username, IconUrl: thumbnailHelper.GetAvatar(user).Url),
        Description: quote,
        Footer: new EmbedFooter(ReplyFooterText));

    public Embed CreateError(IUser user, string description, bool exhausted) => new(
        Title: exhausted ? "AI quota exhausted. A structural caveat." : "AI delivery failed. An implementation detail.",
        Colour: Color.Red,
        Author: new EmbedAuthor(user.Username, IconUrl: thumbnailHelper.GetAvatar(user).Url),
        Fields: new[] { new EmbedField("What went wrong?", description, false) },
        Footer: new EmbedFooter(ReplyFooterText));
}
