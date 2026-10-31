using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Accord.Services.UserBotMessages;
using MediatR;

namespace Accord.Services.AiQuotes;

public sealed record TrackAiReplyRequest(ulong MessageId, ulong ChannelId, ulong CallerId, bool AnyoneCanDelete) : IRequest;
public sealed record DeleteAiReplyRequest(ulong MessageId, ulong ChannelId, ulong UserId) : IRequest<bool>;

[RegisterSingleton]
public class AiReplyStore
{
    internal ConcurrentDictionary<ulong, TrackAiReplyRequest> Replies { get; } = new();
}

public class TrackAiReplyHandler(AiReplyStore store) : IRequestHandler<TrackAiReplyRequest>
{
    public Task Handle(TrackAiReplyRequest request, CancellationToken cancellationToken)
    {
        store.Replies[request.MessageId] = request;
        return Task.CompletedTask;
    }
}

public class DeleteAiReplyHandler(AiReplyStore store, IMediator mediator) : IRequestHandler<DeleteAiReplyRequest, bool>
{
    public async Task<bool> Handle(DeleteAiReplyRequest request, CancellationToken cancellationToken)
    {
        if (!store.Replies.TryGetValue(request.MessageId, out var reply))
            return false;

        // A recognized AI reply never falls through to the general administrator override.
        if (reply.ChannelId != request.ChannelId || (!reply.AnyoneCanDelete && reply.CallerId != request.UserId))
            return true;

        var result = await mediator.Send(new DeleteDiscordMessageRequest(reply.MessageId, reply.ChannelId), cancellationToken);
        if (result.Success)
            store.Replies.TryRemove(reply.MessageId, out _);

        return true;
    }
}
