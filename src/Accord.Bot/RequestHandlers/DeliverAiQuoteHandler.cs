using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.AiQuotes;
using MediatR;
using Remora.Discord.Commands.Feedback.Services;

namespace Accord.Bot.RequestHandlers;

public class DeliverAiQuoteHandler(FeedbackService feedbackService)
    : IRequestHandler<DeliverAiQuoteRequest, DeliverAiQuoteResponse>
{
    public async Task<DeliverAiQuoteResponse> Handle(DeliverAiQuoteRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Give Discord's thinking indicator time to sell the illusion.
        await Task.Delay(Random.Shared.Next(0, 3000), cancellationToken);
        var result = await feedbackService.SendContextualAsync(AiQuoteCatalog.GetRandomQuote(), ct: cancellationToken);
        return new DeliverAiQuoteResponse(result.IsSuccess);
    }
}
