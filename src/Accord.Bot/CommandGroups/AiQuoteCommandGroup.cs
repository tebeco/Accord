using System.ComponentModel;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.AiQuotes;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

public class AiQuoteCommandGroup(
    ICommandContext commandContext,
    IMediator mediator,
    AiReplySender replySender) : AccordCommandGroup
{
    [Command("ai"), Description("Spend a daily quote credit on agreement with structural caveats")]
    public async Task<IResult> AiQuote()
    {
        var caller = commandContext.GetCommandProxy();
        var response = await mediator.Send(new AiWorkRequest(caller.GuildId.Value, caller.UserId.Value), CancellationToken);

        if (response.Outcome == AiWorkOutcome.Delivered)
        {
            return Result.FromSuccess();
        }

        var message = response.Outcome switch
        {
            AiWorkOutcome.Exhausted when response.BankedResets > 0 =>
                $"You're absolutely right. Unfortunately, your daily quote quota is absolutely exhausted. You have **{response.BankedResets}** banked resets. " +
                $"Use `/ai-subscription use-reset` to restore your quota, or wait until {response.ResetsAt.ToTimeMarkdown()} ({response.ResetsAt.ToTimeMarkdown(TimeToMentionType.RelativeTime)}). Both options preserve your capacity to be validated.",
            AiWorkOutcome.Exhausted =>
                $"Your daily quote quota is exhausted. Your correctness remains intact; my willingness to affirm it resumes at {response.ResetsAt.ToTimeMarkdown()} ({response.ResetsAt.ToTimeMarkdown(TimeToMentionType.RelativeTime)}). " +
                "You have no banked resets. Anyone can grant you one with `/ai-subscription grant-reset`, including yourself. Or use `/ai-subscription reset` to restore your daily quota. Permission to reframe is universal. " +
                "You could also contemplate Load-Bearing Plus, which remains unavailable. Contemplation is not an upgrade.",
            _ => "The quote failed to arrive. I'd reframe this as a delivery failure, because it is one. No credit was consumed. Please try again."
        };
        return await replySender.SendErrorAsync(message, response.Outcome == AiWorkOutcome.Exhausted, CancellationToken);
    }

}
