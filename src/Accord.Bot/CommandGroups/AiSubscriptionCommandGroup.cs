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
            var nextStep = isSelf
                ? $"Use `/ai` or `!ai` to start **{AiPlans.Free.Name}**."
                : "They can use `/ai` or `!ai` to start their free plan.";
            return await replySender.SendSubscriptionAsync($"**No active subscription**\n{nextStep}", CancellationToken, user);
        }

        const int barWidth = 5;
        var filledSegments = subscription.SuccessfulQuotes * barWidth / subscription.Plan.DailyQuotes;
        var progressBar = new string('█', filledSegments) + new string('░', barWidth - filledSegments);

        var usageDescription = $"""
            **AI subscription usage**
            *{subscription.Plan.Name}*

            **Daily quotes**
            `{progressBar}` **{subscription.SuccessfulQuotes}/{subscription.Plan.DailyQuotes}** used · **{subscription.RemainingQuotes}** left
            Resets {subscription.ResetsAt.ToTimeMarkdown(TimeToMentionType.RelativeTime)} ({subscription.ResetsAt.ToTimeMarkdown()})

            **Banked resets** · **{subscription.BankedResets}**
            """;
        return await replySender.SendSubscriptionAsync(usageDescription, CancellationToken, user);
    }

    [Command("reset"), Description("Reset a user's daily quota; reframe exhaustion as available nuance")]
    public async Task<IResult> Reset([Description("Whose daily quota needs a fresh framework; defaults to yourself")] IUser? user = null)
    {
        var caller = commandContext.GetCommandProxy();
        var userId = user?.ID ?? caller.UserId;
        var isSelf = userId == caller.UserId;
        var response = await mediator.Send(new ResetAiUsageRequest(caller.GuildId.Value, userId.Value), CancellationToken);
        if (response.Subscription is not { } subscription)
        {
            var nextStep = isSelf
                ? "Use `/ai` or `!ai` to start your free plan."
                : "They can use `/ai` or `!ai` to start their free plan.";
            return await replySender.SendSubscriptionAsync($"**No active subscription**\n{nextStep}", CancellationToken, user);
        }

        var heading = isSelf ? "Your daily quota was reset" : "Their daily quota was reset";
        var description = $"""
            **{heading}**
            *{subscription.Plan.Name}*

            **{subscription.RemainingQuotes}/{subscription.Plan.DailyQuotes}** quotes available
            **Banked resets** · **{subscription.BankedResets}**
            """;
        return await replySender.SendSubscriptionAsync(description, CancellationToken, user);
    }

    [Command("use-reset"), Description("Spend one banked reset on an exhausted daily quota; operationalize your optionality")]
    public async Task<IResult> UseReset()
    {
        var caller = commandContext.GetCommandProxy();
        var response = await mediator.Send(new UseBankedAiResetRequest(caller.GuildId.Value, caller.UserId.Value), CancellationToken);
        var description = (response.Outcome, response.Subscription) switch
        {
            (BankedAiResetOutcome.Used, { } subscription) => $"""
                **One banked reset spent**

                **{subscription.RemainingQuotes}/{subscription.Plan.DailyQuotes}** quotes available
                **Banked resets** · **{subscription.BankedResets}**
                Next daily reset {subscription.ResetsAt.ToTimeMarkdown(TimeToMentionType.RelativeTime)} ({subscription.ResetsAt.ToTimeMarkdown()})

                Use `/ai` or `!ai` to get another quote.
                """,
            (BankedAiResetOutcome.QuotaAvailable, { } subscription) => $"""
                **You still have quotes left**

                **{subscription.RemainingQuotes}/{subscription.Plan.DailyQuotes}** quotes available
                **Banked resets** · **{subscription.BankedResets}**

                You can spend a banked reset once you run out of quotes.
                """,
            (BankedAiResetOutcome.NoBankedResets, { } subscription) => $"""
                **No banked resets available**

                Your daily quota refills {subscription.ResetsAt.ToTimeMarkdown(TimeToMentionType.RelativeTime)} ({subscription.ResetsAt.ToTimeMarkdown()}).

                • Refill now with `/ai-subscription reset`.
                • Bank a reset with `/ai-subscription grant-reset`. You can grant one to yourself or ask someone to grant you one.
                """,
            (BankedAiResetOutcome.NotEnrolled, _) => $"**No active subscription**\nUse `/ai` or `!ai` to start **{AiPlans.Free.Name}**.",
            _ => "**Couldn’t use a banked reset**\nPlease try again."
        };
        return await replySender.SendSubscriptionAsync(description, CancellationToken);
    }

    [Command("grant-reset"), Description("Grant a user one banked reset; fund their emergency reserve of caveats")]
    public async Task<IResult> GrantReset([Description("User receiving one banked reset and a renewed sense of optionality")] IUser user)
    {
        var caller = commandContext.GetCommandProxy();
        var isSelf = user.ID == caller.UserId;
        var response = await mediator.Send(new GrantBankedAiResetRequest(caller.GuildId.Value, user.ID.Value), CancellationToken);
        if (response.Subscription is not { } subscription)
        {
            var nextStep = isSelf
                ? "Use `/ai` or `!ai` to enroll before banking a reset."
                : "They need to use `/ai` or `!ai` before they can receive a reset.";
            return await replySender.SendSubscriptionAsync($"**No active subscription**\n{nextStep}", CancellationToken, user);
        }

        var resetHint = isSelf
            ? "Use `/ai-subscription use-reset` when you run out of quotes."
            : "They can use `/ai-subscription use-reset` when they run out of quotes.";
        var description = $"""
            **One reset added to the bank**

            **Banked resets** · **{subscription.BankedResets}**
            **Daily quotes left** · **{subscription.RemainingQuotes}**

            {resetHint}
            """;
        return await replySender.SendSubscriptionAsync(description, CancellationToken, user);
    }

    [Command("plans"), Description("Browse quote quotas and increasingly theoretical access to more nuance")]
    public async Task<IResult> Plans()
    {
        var response = await mediator.Send(new GetAiPlansRequest(), CancellationToken);
        var plans = string.Join("\n\n", response.Plans.Select(plan =>
            $"**{plan.Name}**\n*{plan.DailyQuotes} quotes per day*\n{plan.Availability}"));
        return await replySender.SendSubscriptionAsync(plans, CancellationToken);
    }

    [Command("upgrade"), Description("Request an upgrade and receive a carefully qualified refusal from imaginary billing")]
    public async Task<IResult> Upgrade()
        => await replySender.SendSubscriptionAsync(
            "You're absolutely right to want more. Our imaginary billing department is on leave, so your upgrade has been reframed as an aspiration. " +
            "No payment, no plan change, no extra quota. Your ambition is valid; its implementation remains hypothetical.", ct: CancellationToken);
}
