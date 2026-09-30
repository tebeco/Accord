using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Accord.Domain.Model;
using Accord.Services.Permissions;
using Accord.Services.RunOptions;
using MediatR;

namespace Accord.Services.Spam;

public sealed record AddSpamCheckMessageRequest(
    ulong DiscordMessageId,
    ulong DiscordMessageChannelId,
    string Content,
    ulong DiscordUserId
) : IRequest;

internal class AddSpamCheckMessageHandler(
    RunOptionService runOptionService,
    SpamAnalysisService spamAnalysis,
    UserPermissionService userPermissionService,
    IPermissionUserProvider permissionUserProvider,
    IMediator mediator)
    : IRequestHandler<AddSpamCheckMessageRequest>
{
    public async Task Handle(AddSpamCheckMessageRequest request, CancellationToken cancellationToken)
    {
        var enableSpamMute = await runOptionService.GetOption<bool>(RunOptionKey.SpamMuteEnabled);
        if (!enableSpamMute)
        {
            return;
        }

        var user = await permissionUserProvider.GetPermissionUser(request.DiscordUserId, cancellationToken);
        if (await userPermissionService.HasPermission(user, PermissionType.BypassSpamCheck))
        {
            return;
        }

        var spamMessageThreshold = await runOptionService.GetOption<int>(RunOptionKey.SpamMessageThreshold);
        var spamMessageWindowInSeconds = await runOptionService.GetOption<int>(RunOptionKey.SpamMessageWindowInSeconds);
        var matches = spamAnalysis.TryGetSpamMatches(
            request.DiscordUserId,
            request.DiscordMessageId,
            request.DiscordMessageChannelId,
            request.Content,
            spamMessageThreshold,
            spamMessageWindowInSeconds);

        if (matches.Count == 0)
        {
            return;
        }

        var timeoutInSeconds = await runOptionService.GetOption<int>(RunOptionKey.SpamTimeoutInSeconds);
        var allMessages = matches
            .Append(new SpamMatch(request.DiscordMessageId, request.DiscordMessageChannelId))
            .ToList();

        await mediator.Publish(
            new CleanUpSpamInDiscordRequest(request.DiscordUserId, allMessages, timeoutInSeconds),
            cancellationToken);
    }
}
