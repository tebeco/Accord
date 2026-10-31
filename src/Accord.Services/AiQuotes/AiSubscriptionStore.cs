using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Accord.Services.AiQuotes;

[RegisterSingleton]
public class AiSubscriptionStore
{
    private readonly ConcurrentDictionary<(ulong GuildId, ulong UserId), Subscriber> _subscribers = new();

    internal async Task<AiWorkResponse> WorkAsync(
        ulong guildId,
        ulong userId,
        Func<CancellationToken, Task<DeliverAiQuoteResponse>> deliver,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var subscriber = _subscribers.GetOrAdd((guildId, userId), static _ => new Subscriber());
        await subscriber.Gate.WaitAsync(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var now = DateTimeOffset.UtcNow;
            subscriber.Subscription ??= new AiSubscription(AiPlans.Free, now, DateOnly.FromDateTime(now.UtcDateTime), 0);
            subscriber.Subscription = RollOver(subscriber.Subscription, now);

            if (subscriber.Subscription.RemainingQuotes == 0)
            {
                return new AiWorkResponse(AiWorkOutcome.Exhausted, subscriber.Subscription.BankedResets, subscriber.Subscription.ResetsAt);
            }

            // Hold this subscriber's gate through delivery so concurrent commands cannot overspend.
            var delivery = await deliver(cancellationToken);
            if (!delivery.Success)
            {
                return new AiWorkResponse(AiWorkOutcome.DeliveryFailed);
            }

            // Charge a confirmed delivery even if cancellation arrives just after it succeeds.
            // A delivery crossing midnight belongs to the day on which it completes.
            var subscription = RollOver(subscriber.Subscription, DateTimeOffset.UtcNow);
            subscriber.Subscription = subscription with { SuccessfulQuotes = subscription.SuccessfulQuotes + 1 };
            return new AiWorkResponse(AiWorkOutcome.Delivered);
        }
        finally
        {
            subscriber.Gate.Release();
        }
    }

    internal async Task<AiSubscription?> GetUsageAsync(ulong guildId, ulong userId, CancellationToken cancellationToken)
    {
        if (!_subscribers.TryGetValue((guildId, userId), out var subscriber))
        {
            return null;
        }

        await subscriber.Gate.WaitAsync(cancellationToken);
        try
        {
            if (subscriber.Subscription is { } subscription)
            {
                subscriber.Subscription = RollOver(subscription, DateTimeOffset.UtcNow);
            }

            return subscriber.Subscription;
        }
        finally
        {
            subscriber.Gate.Release();
        }
    }

    internal async Task<AiSubscription?> ResetUsageAsync(ulong guildId, ulong userId, CancellationToken cancellationToken)
    {
        if (!_subscribers.TryGetValue((guildId, userId), out var subscriber))
        {
            return null;
        }

        // Share the delivery gate so an in-flight quote finishes charging before its usage is reset.
        await subscriber.Gate.WaitAsync(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (subscriber.Subscription is not { } subscription)
            {
                return null;
            }

            subscriber.Subscription = subscription with
            {
                UsageDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime),
                SuccessfulQuotes = 0
            };
            return subscriber.Subscription;
        }
        finally
        {
            subscriber.Gate.Release();
        }
    }

    internal async Task<BankedAiResetResponse> UseBankedResetAsync(ulong guildId, ulong userId, CancellationToken cancellationToken)
    {
        if (!_subscribers.TryGetValue((guildId, userId), out var subscriber))
        {
            return new BankedAiResetResponse(BankedAiResetOutcome.NotEnrolled);
        }

        await subscriber.Gate.WaitAsync(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (subscriber.Subscription is not { } subscription)
            {
                return new BankedAiResetResponse(BankedAiResetOutcome.NotEnrolled);
            }

            // Midnight restores the daily quota for free; never spend a banked reset on that rollover.
            subscription = RollOver(subscription, DateTimeOffset.UtcNow);
            subscriber.Subscription = subscription;
            if (subscription.RemainingQuotes > 0)
            {
                return new BankedAiResetResponse(BankedAiResetOutcome.QuotaAvailable, subscription);
            }

            if (subscription.BankedResets == 0)
            {
                return new BankedAiResetResponse(BankedAiResetOutcome.NoBankedResets, subscription);
            }

            subscriber.Subscription = subscription with
            {
                SuccessfulQuotes = 0,
                BankedResets = subscription.BankedResets - 1
            };
            return new BankedAiResetResponse(BankedAiResetOutcome.Used, subscriber.Subscription);
        }
        finally
        {
            subscriber.Gate.Release();
        }
    }

    internal async Task<BankedAiResetResponse> GrantBankedResetAsync(ulong guildId, ulong userId, CancellationToken cancellationToken)
    {
        if (!_subscribers.TryGetValue((guildId, userId), out var subscriber))
        {
            return new BankedAiResetResponse(BankedAiResetOutcome.NotEnrolled);
        }

        await subscriber.Gate.WaitAsync(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (subscriber.Subscription is not { } subscription)
            {
                return new BankedAiResetResponse(BankedAiResetOutcome.NotEnrolled);
            }

            subscription = RollOver(subscription, DateTimeOffset.UtcNow);
            subscriber.Subscription = subscription with { BankedResets = checked(subscription.BankedResets + 1) };
            return new BankedAiResetResponse(BankedAiResetOutcome.Granted, subscriber.Subscription);
        }
        finally
        {
            subscriber.Gate.Release();
        }
    }

    private static AiSubscription RollOver(AiSubscription subscription, DateTimeOffset now)
    {
        var date = DateOnly.FromDateTime(now.UtcDateTime);
        return subscription.UsageDate == date
            ? subscription
            : subscription with { UsageDate = date, SuccessfulQuotes = 0 };
    }

    private sealed class Subscriber
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public AiSubscription? Subscription { get; set; }
    }
}
