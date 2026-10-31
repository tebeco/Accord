using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.AiQuotes;
using MediatR;

namespace Accord.Bot.RequestHandlers;

public class DeliverAiQuoteHandler(AiReplySender replySender)
    : IRequestHandler<DeliverAiQuoteRequest, DeliverAiQuoteResponse>
{
    public async Task<DeliverAiQuoteResponse> Handle(DeliverAiQuoteRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Give Discord's thinking indicator time to sell the illusion.
        await Task.Delay(Random.Shared.Next(0, 3000), cancellationToken);
        var result = await replySender.SendQuoteAsync(AiQuoteCatalog.GetRandomQuote(), cancellationToken);
        return new DeliverAiQuoteResponse(result.IsSuccess);
    }
}
