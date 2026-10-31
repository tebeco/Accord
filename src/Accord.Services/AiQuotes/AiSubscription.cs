using System;
using System.Collections.Generic;

namespace Accord.Services.AiQuotes;

public sealed record AiPlan(string Name, int DailyQuotes, string Availability);

public static class AiPlans
{
    public static AiPlan Free { get; } = new("Free Trial of Being Absolutely Right", 5,
        "Automatically assigned on your first quote. Your intuition is sound; your daily allowance is five.");

    public static IReadOnlyList<AiPlan> All { get; } = Array.AsReadOnly<AiPlan>(
    [
        Free,
        new AiPlan("Load-Bearing Plus", 20, "Permanently coming soon. The promise is load-bearing; the availability is hypothetical."),
        new AiPlan("It Depends Pro Max", 100, "Unavailable. Our imaginary billing department is still unpacking what billing means.")
    ]);
}

public sealed record AiSubscription(
    AiPlan Plan,
    DateTimeOffset CreatedAt,
    DateOnly UsageDate,
    int SuccessfulQuotes,
    int BankedResets = 0)
{
    public int RemainingQuotes => Math.Max(0, Plan.DailyQuotes - SuccessfulQuotes);

    public DateTimeOffset ResetsAt => new(UsageDate.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
}
