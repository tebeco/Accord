using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.AiQuotes;

public enum BankedAiResetOutcome
{
    NotEnrolled,
    QuotaAvailable,
    NoBankedResets,
    Used,
    Granted
}

public sealed record UseBankedAiResetRequest(ulong GuildId, ulong UserId) : IRequest<BankedAiResetResponse>;
public sealed record GrantBankedAiResetRequest(ulong GuildId, ulong UserId) : IRequest<BankedAiResetResponse>;
public sealed record BankedAiResetResponse(BankedAiResetOutcome Outcome, AiSubscription? Subscription = null);

public class UseBankedAiResetHandler(AiSubscriptionStore store) : IRequestHandler<UseBankedAiResetRequest, BankedAiResetResponse>
{
    public Task<BankedAiResetResponse> Handle(UseBankedAiResetRequest request, CancellationToken cancellationToken)
        => store.UseBankedResetAsync(request.GuildId, request.UserId, cancellationToken);
}

public class GrantBankedAiResetHandler(AiSubscriptionStore store) : IRequestHandler<GrantBankedAiResetRequest, BankedAiResetResponse>
{
    public Task<BankedAiResetResponse> Handle(GrantBankedAiResetRequest request, CancellationToken cancellationToken)
        => store.GrantBankedResetAsync(request.GuildId, request.UserId, cancellationToken);
}
