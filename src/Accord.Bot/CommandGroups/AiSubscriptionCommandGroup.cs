using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.AiQuotes;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

[Group("ai-subscription")]
public class AiSubscriptionCommandGroup(
    ICommandContext commandContext,
    IMediator mediator,
    FeedbackService feedbackService) : AccordCommandGroup
{
    [Command("usage"), Description("Inspect your or another user's subscription and daily nuance allowance")]
    public async Task<IResult> Usage([Description("User to look up (defaults to yourself)")] IUser? user = null)
    {
        var caller = commandContext.GetCommandProxy();
        var userId = user?.ID ?? caller.UserId;
        var isSelf = userId == caller.UserId;
        var response = await mediator.Send(new GetAiUsageRequest(caller.GuildId.Value, userId.Value), CancellationToken);
        if (response.Subscription is not { } subscription)
        {
            return await feedbackService.SendContextualAsync(
                isSelf
                    ? "Your thoughts are currently unsubscribed. Use `/ai` to start your Free Trial of Thought."
                    : $"User `{userId.Value}` has no AI subscription in this server yet. They must use `/ai` or `!ai` first.",
                ct: CancellationToken);
        }

        var resetHint = isSelf
            ? "Use `/ai-subscription use-reset` when your daily quota is exhausted."
            : "They can use `/ai-subscription use-reset` when their daily quota is exhausted.";

        return await feedbackService.SendContextualAsync(
            $"AI usage for user `{userId.Value}`\n" +
            $"**{subscription.Plan.Name}**\n" +
            $"Daily quotes used: **{subscription.SuccessfulQuotes}/{subscription.Plan.DailyQuotes}**\n" +
            $"Remaining nuance: **{subscription.RemainingQuotes}**\n" +
            $"Banked resets: **{subscription.BankedResets}**\n" +
            $"{resetHint}\n" +
            $"Resets: **{subscription.ResetsAt:yyyy-MM-dd HH:mm} UTC**", ct: CancellationToken);
    }

    [Command("reset"), Description("Reset a user's daily AI quota in this server")]
    [RequireDiscordPermission(DiscordPermission.Administrator)]
    public async Task<IResult> Reset([Description("User whose daily AI quota should be reset")] IUser user)
    {
        var caller = commandContext.GetCommandProxy();
        var response = await mediator.Send(new ResetAiUsageRequest(caller.GuildId.Value, user.ID.Value), CancellationToken);
        if (response.Subscription is not { } subscription)
        {
            return await feedbackService.SendContextualAsync(
                $"User `{user.ID.Value}` has no AI subscription in this server yet. There is no nuance to restore.", ct: CancellationToken);
        }

        return await feedbackService.SendContextualAsync(
            $"Daily AI usage reset for user `{user.ID.Value}`. " +
            $"**{subscription.RemainingQuotes}/{subscription.Plan.DailyQuotes}** quotes available on **{subscription.Plan.Name}**. " +
            "Their capacity for nuance has been restored.", ct: CancellationToken);
    }

    [Command("use-reset"), Description("Spend one banked reset to restore your exhausted daily AI quota")]
    public async Task<IResult> UseReset()
    {
        var caller = commandContext.GetCommandProxy();
        var response = await mediator.Send(new UseBankedAiResetRequest(caller.GuildId.Value, caller.UserId.Value), CancellationToken);
        var message = response.Outcome switch
        {
            BankedAiResetOutcome.NotEnrolled => "Your thoughts are currently unsubscribed. Use `/ai` to start your Free Trial of Thought.",
            BankedAiResetOutcome.QuotaAvailable =>
                $"You still have **{response.Subscription!.RemainingQuotes}** quotes available. No banked reset was spent. Preserve your emergency nuance.",
            BankedAiResetOutcome.NoBankedResets =>
                "Your bank of nuance is empty. Ask an administrator for a banked reset, or wait until midnight UTC.",
            BankedAiResetOutcome.Used =>
                $"One banked reset spent. **{response.Subscription!.RemainingQuotes}** quotes restored; **{response.Subscription.BankedResets}** banked resets remain. " +
                "Use `/ai` or `!ai` to resume being absolutely right.",
            _ => "Your banked reset could not be used. Please try again."
        };
        return await feedbackService.SendContextualAsync(message, ct: CancellationToken);
    }

    [Command("grant-reset"), Description("Grant a user one banked AI reset in this server")]
    [RequireDiscordPermission(DiscordPermission.Administrator)]
    public async Task<IResult> GrantReset([Description("User who should receive one banked AI reset")] IUser user)
    {
        var caller = commandContext.GetCommandProxy();
        var response = await mediator.Send(new GrantBankedAiResetRequest(caller.GuildId.Value, user.ID.Value), CancellationToken);
        if (response.Outcome == BankedAiResetOutcome.NotEnrolled)
        {
            return await feedbackService.SendContextualAsync(
                $"User `{user.ID.Value}` has no AI subscription in this server yet. They must use `/ai` or `!ai` first.", ct: CancellationToken);
        }

        return await feedbackService.SendContextualAsync(
            $"Granted one banked reset to user `{user.ID.Value}`. Their emergency nuance reserve now contains **{response.Subscription!.BankedResets}** resets. " +
            "Daily usage has not been reset.", ct: CancellationToken);
    }

    [Command("plans"), Description("Browse plans with increasingly load-bearing promises")]
    public async Task<IResult> Plans()
    {
        var response = await mediator.Send(new GetAiPlansRequest(), CancellationToken);
        var plans = string.Join("\n\n", response.Plans.Select(plan =>
            $"**{plan.Name}** — {plan.DailyQuotes} quotes/day\n{plan.Availability}"));
        return await feedbackService.SendContextualAsync(
            $"{plans}", ct: CancellationToken);
    }

    [Command("upgrade"), Description("Consult our permanently unavailable billing department")]
    public async Task<IResult> Upgrade()
        => await feedbackService.SendContextualAsync(
            "Our billing department is on leave. Your ambition has been recorded in volatile memory.", ct: CancellationToken);
}
