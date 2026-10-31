using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Accord.Services.AiQuotes;

public enum AiWorkOutcome
{
    Delivered,
    Exhausted,
    DeliveryFailed
}

public sealed record AiWorkRequest(ulong GuildId, ulong UserId) : IRequest<AiWorkResponse>;
public sealed record AiWorkResponse(AiWorkOutcome Outcome, int BankedResets = 0, DateTimeOffset ResetsAt = default);
public sealed record GetAiUsageRequest(ulong GuildId, ulong UserId) : IRequest<GetAiUsageResponse>;
public sealed record GetAiUsageResponse(AiSubscription? Subscription);
public sealed record GetAiPlansRequest : IRequest<GetAiPlansResponse>;
public sealed record GetAiPlansResponse(IReadOnlyList<AiPlan> Plans);

// Implemented by Accord.Bot, which owns quote selection and contextual Discord delivery.
public sealed record DeliverAiQuoteRequest : IRequest<DeliverAiQuoteResponse>;
public sealed record DeliverAiQuoteResponse(bool Success);

public class AiWorkHandler(AiSubscriptionStore store, IMediator mediator) : IRequestHandler<AiWorkRequest, AiWorkResponse>
{
    public Task<AiWorkResponse> Handle(AiWorkRequest request, CancellationToken cancellationToken)
        => store.WorkAsync(request.GuildId, request.UserId,
            ct => mediator.Send(new DeliverAiQuoteRequest(), ct), cancellationToken);
}

public class GetAiUsageHandler(AiSubscriptionStore store) : IRequestHandler<GetAiUsageRequest, GetAiUsageResponse>
{
    public async Task<GetAiUsageResponse> Handle(GetAiUsageRequest request, CancellationToken cancellationToken)
        => new(await store.GetUsageAsync(request.GuildId, request.UserId, cancellationToken));
}

public class GetAiPlansHandler : IRequestHandler<GetAiPlansRequest, GetAiPlansResponse>
{
    public Task<GetAiPlansResponse> Handle(GetAiPlansRequest request, CancellationToken cancellationToken)
        => Task.FromResult(new GetAiPlansResponse(AiPlans.All));
}
