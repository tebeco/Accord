using System.Threading;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.Helpers;

[RegisterScoped]
public class AiReplySender(
    FeedbackService feedbackService,
    ICommandContext commandContext,
    AiEmbedFactory embedFactory)
{
    public async Task<IResult> SendSubscriptionAsync(string description, CancellationToken ct, IUser? subject = null)
        => await feedbackService.SendContextualEmbedAsync(
            embedFactory.CreateProfile(subject ?? commandContext.GetExecutingUser(), description), ct: ct);

    public async Task<IResult> SendQuoteAsync(string quote, CancellationToken ct)
        => await feedbackService.SendContextualEmbedAsync(
            embedFactory.CreateQuote(commandContext.GetExecutingUser(), quote), ct: ct);

    public async Task<IResult> SendErrorAsync(string description, bool exhausted, CancellationToken ct)
        => await feedbackService.SendContextualEmbedAsync(
            embedFactory.CreateError(commandContext.GetExecutingUser(), description, exhausted), ct: ct);
}
