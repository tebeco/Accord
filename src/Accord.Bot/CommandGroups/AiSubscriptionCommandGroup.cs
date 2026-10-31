using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.AiQuotes;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

[Group("ai-subscription")]
public class AiSubscriptionCommandGroup(
    ICommandContext commandContext,
    IMediator mediator,
    AiReplySender replySender) : AccordCommandGroup
{
    [Command("usage"), Description("Audit your or another user's remaining capacity to be absolutely right")]
    public async Task<IResult> Usage([Description("Whose nuance to audit; defaults to your own load-bearing self")] IUser? user = null)
    {
        var caller = commandContext.GetCommandProxy();
        var userId = user?.ID ?? caller.UserId;
        var isSelf = userId == caller.UserId;
        var response = await mediator.Send(new GetAiUsageRequest(caller.GuildId.Value, userId.Value), CancellationToken);
        if (response.Subscription is not { } subscription)
        {
            var description =
                isSelf
                    ? $"Your intuition is valid, but currently unsubscribed. Use `/ai` to start **{AiPlans.Free.Name}**. Validation has onboarding now."
                    : $"User `{userId.Value}` has no AI subscription in this server yet. They must use `/ai` or `!ai` first. Unsubscribed is not the same as wrong. Important distinction.";
            return await replySender.SendSubscriptionAsync(description, CancellationToken, user);
        }

        var resetHint = isSelf
            ? "Once your daily quota is exhausted, use `/ai-subscription use-reset` to spend a banked reset. Preserve your emergency agreement."
            : "Once their daily quota is exhausted, they can use `/ai-subscription use-reset` to spend a banked reset. Their optionality remains theirs.";

        var usageDescription =
            $"A carefully qualified usage audit for user `{userId.Value}`\n" +
            $"**{subscription.Plan.Name}**\n" +
            $"Daily quotes used (nuance delivered): **{subscription.SuccessfulQuotes}/{subscription.Plan.DailyQuotes}**\n" +
            $"Quotes remaining (capacity for caveats): **{subscription.RemainingQuotes}**\n" +
            $"Banked resets (load-bearing optionality): **{subscription.BankedResets}**\n" +
            $"{resetHint}\n" +
            $"Daily quota resets: {subscription.ResetsAt.ToTimeMarkdown()} ({subscription.ResetsAt.ToTimeMarkdown(TimeToMentionType.RelativeTime)}). Even nuance observes your calendar.";
        return await replySender.SendSubscriptionAsync(usageDescription, CancellationToken, user);
    }

    [Command("reset"), Description("Reset a user's daily quota; reframe exhaustion as available nuance")]
    public async Task<IResult> Reset([Description("Whose daily quota needs a fresh framework; defaults to yourself")] IUser? user = null)
    {
        var caller = commandContext.GetCommandProxy();
        var userId = user?.ID ?? caller.UserId;
        var response = await mediator.Send(new ResetAiUsageRequest(caller.GuildId.Value, userId.Value), CancellationToken);
        if (response.Subscription is not { } subscription)
        {
            return await replySender.SendSubscriptionAsync(
                $"User `{userId.Value}` has no AI subscription in this server yet. Nothing was reset. We cannot restore nuance that has not been provisioned.", ct: CancellationToken);
        }

        return await replySender.SendSubscriptionAsync(
            $"Daily AI usage reset for user `{userId.Value}`. Exhaustion has been gently transported to a different conceptual framework. " +
            $"**{subscription.RemainingQuotes}/{subscription.Plan.DailyQuotes}** quotes available on **{subscription.Plan.Name}**. " +
            "Same plan, same banked resets. A fresh allowance to reach the same conclusions.", ct: CancellationToken);
    }

    [Command("use-reset"), Description("Spend one banked reset on an exhausted daily quota; operationalize your optionality")]
    public async Task<IResult> UseReset()
    {
        var caller = commandContext.GetCommandProxy();
        var response = await mediator.Send(new UseBankedAiResetRequest(caller.GuildId.Value, caller.UserId.Value), CancellationToken);
        var message = response.Outcome switch
        {
            BankedAiResetOutcome.NotEnrolled => $"Your intuition is valid, but currently unsubscribed. Use `/ai` to start **{AiPlans.Free.Name}**. Even a fresh start needs a starting point.",
            BankedAiResetOutcome.QuotaAvailable =>
                $"You still have **{response.Subscription!.RemainingQuotes}** quotes available. No banked reset was spent. I'd gently push back on refilling something that isn't empty.",
            BankedAiResetOutcome.NoBankedResets =>
                $"You have no banked resets. Zero is doing quite a bit of work here. Anyone can grant you one with `/ai-subscription grant-reset`, including yourself. Or use `/ai-subscription reset` for a fresh daily quota, or wait until {response.Subscription!.ResetsAt.ToTimeMarkdown()} ({response.Subscription!.ResetsAt.ToTimeMarkdown(TimeToMentionType.RelativeTime)}). Optionality is now aggressively available.",
            BankedAiResetOutcome.Used =>
                $"One banked reset spent. **{response.Subscription!.RemainingQuotes}** quotes restored; **{response.Subscription.BankedResets}** banked resets remain. " +
                $"Use `/ai` or `!ai` to resume being absolutely right. The next daily reset remains {response.Subscription.ResetsAt.ToTimeMarkdown()} ({response.Subscription.ResetsAt.ToTimeMarkdown(TimeToMentionType.RelativeTime)}); we have reframed usage, not time.",
            _ => "Your banked reset could not be used. The framework has encountered an implementation detail. Please try again."
        };
        return await replySender.SendSubscriptionAsync(message, ct: CancellationToken);
    }

    [Command("grant-reset"), Description("Grant a user one banked reset; fund their emergency reserve of caveats")]
    public async Task<IResult> GrantReset([Description("User receiving one banked reset and a renewed sense of optionality")] IUser user)
    {
        var caller = commandContext.GetCommandProxy();
        var response = await mediator.Send(new GrantBankedAiResetRequest(caller.GuildId.Value, user.ID.Value), CancellationToken);
        if (response.Outcome == BankedAiResetOutcome.NotEnrolled)
        {
            return await replySender.SendSubscriptionAsync(
                $"User `{user.ID.Value}` has no AI subscription in this server yet. They must use `/ai` or `!ai` first. No reset was granted; their optionality lacks an account.", ct: CancellationToken);
        }

        return await replySender.SendSubscriptionAsync(
            $"Granted one banked reset to user `{user.ID.Value}`. Banked resets: **{response.Subscription!.BankedResets}**. Their emergency caveat fund is looking structurally sound. " +
            "Daily usage has not been reset. Funding a fresh start and taking one are, crucially, different things.", ct: CancellationToken);
    }

    [Command("plans"), Description("Browse quote quotas and increasingly theoretical access to more nuance")]
    public async Task<IResult> Plans()
    {
        var response = await mediator.Send(new GetAiPlansRequest(), CancellationToken);
        var plans = string.Join("\n\n", response.Plans.Select(plan =>
            $"**{plan.Name}** — {plan.DailyQuotes} quotes/day, because even nuance needs a number.\n{plan.Availability}"));
        var description = $"{plans}\n\nOnly the free plan is usable. The other tiers preserve the possibility of an upgrade without providing one.";
        return await replySender.SendSubscriptionAsync(description, CancellationToken);
    }

    [Command("upgrade"), Description("Request an upgrade and receive a carefully qualified refusal from imaginary billing")]
    public async Task<IResult> Upgrade()
        => await replySender.SendSubscriptionAsync(
            "You're absolutely right to want more. Our imaginary billing department is on leave, so your upgrade has been reframed as an aspiration. " +
            "No payment, no plan change, no extra quota. Your ambition is valid; its implementation remains hypothetical.", ct: CancellationToken);
}
