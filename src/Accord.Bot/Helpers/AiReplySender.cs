using System.Threading;
using System.Threading.Tasks;
using Accord.Services.AiQuotes;
using MediatR;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.Helpers;

[RegisterScoped]
public class AiReplySender(
    FeedbackService feedbackService,
    ICommandContext commandContext,
    AiEmbedFactory embedFactory,
    IMediator mediator)
{
    public Task<IResult> SendSubscriptionAsync(string description, CancellationToken ct, IUser? subject = null)
        => SendAsync(embedFactory.CreateProfile(subject ?? commandContext.GetExecutingUser(), description), true, ct);

    public Task<IResult> SendQuoteAsync(string quote, CancellationToken ct)
        => SendAsync(embedFactory.CreateQuote(commandContext.GetExecutingUser(), quote), false, ct);

    public Task<IResult> SendErrorAsync(string description, bool exhausted, CancellationToken ct)
        => SendAsync(embedFactory.CreateError(commandContext.GetExecutingUser(), description, exhausted), false, ct);

    private async Task<IResult> SendAsync(Embed embed, bool anyoneCanDelete, CancellationToken ct)
    {
        var result = await feedbackService.SendContextualEmbedAsync(embed, ct: ct);
        if (result.IsSuccess)
        {
            // Delivery already succeeded: register deletion even if the command is now cancelled.
            await mediator.Send(new TrackAiReplyRequest(result.Entity.ID.Value, result.Entity.ChannelID.Value,
                commandContext.GetCommandProxy().UserId.Value, anyoneCanDelete), CancellationToken.None);
        }

        return result;
    }
}
