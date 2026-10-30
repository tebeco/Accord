using System.ComponentModel;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.AiQuotes;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

namespace Accord.Bot.CommandGroups;

public class AiQuoteCommandGroup(
    ICommandContext commandContext,
    IMediator mediator,
    FeedbackService feedbackService) : AccordCommandGroup
{
    [Command("ai"), Description("Spend your daily allowance of nuance on an AI pushback quote")]
    public async Task<IResult> AiQuote()
    {
        var caller = commandContext.GetCommandProxy();
        var response = await mediator.Send(new AiWorkRequest(caller.GuildId.Value, caller.UserId.Value), CancellationToken);

        return response.Outcome switch
        {
            AiWorkOutcome.Delivered => Result.FromSuccess(),
            AiWorkOutcome.Exhausted when response.BankedResets > 0 => await feedbackService.SendContextualAsync(
                $"You’ve exhausted your daily allowance of nuance. You have **{response.BankedResets}** banked resets. " +
                "Use `/ai-subscription use-reset` to restore your quota, or wait until midnight UTC.", ct: CancellationToken),
            AiWorkOutcome.Exhausted => await feedbackService.SendContextualAsync(
                "You’ve exhausted your daily allowance of nuance. Further agreement resumes at midnight UTC. " +
                "Your bank of nuance is empty; an administrator can grant you a banked reset. " +
                "Consider Load-Bearing Plus, currently unavailable for structural reasons.", ct: CancellationToken),
            _ => await feedbackService.SendContextualAsync(
                "Your nuance could not be delivered. No credit was consumed. Please try again.", ct: CancellationToken)
        };
    }

}
