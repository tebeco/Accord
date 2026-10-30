using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.AiQuotes;

public sealed record ResetAiUsageRequest(ulong GuildId, ulong UserId) : IRequest<ResetAiUsageResponse>;
public sealed record ResetAiUsageResponse(AiSubscription? Subscription);

public class ResetAiUsageHandler(AiSubscriptionStore store) : IRequestHandler<ResetAiUsageRequest, ResetAiUsageResponse>
{
    public async Task<ResetAiUsageResponse> Handle(ResetAiUsageRequest request, CancellationToken cancellationToken)
        => new ResetAiUsageResponse(await store.ResetUsageAsync(request.GuildId, request.UserId, cancellationToken));
}
