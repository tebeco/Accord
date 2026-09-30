using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.Rss;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;

namespace Accord.Bot.HostedServices;

public class RssPollingHostedService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<RssPollingHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var retryDelay = TimeSpan.FromSeconds(1);
        var maxRetryDelay = TimeSpan.FromSeconds(60);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceScopeFactory.CreateScope();
                var services = scope.ServiceProvider;

                var mediator = services.GetRequiredService<IMediator>();
                var channelApi = services.GetRequiredService<IDiscordRestChannelAPI>();

                var feedIds = await mediator.Send(new GetFeedIdsToReadRequest(), stoppingToken);

                foreach (var feedId in feedIds)
                {
                    var result = await mediator.Send(new GetNewPostsFromFeedRequest(feedId), stoppingToken);

                    foreach (var post in result.NewPosts)
                    {
                        await channelApi.CreateMessageAsync(new Snowflake(result.DiscordChannelId),
                            $"**{post.Title}**{Environment.NewLine}{post.Url}",
                            ct: stoppingToken);
                    }
                }

                retryDelay = TimeSpan.FromSeconds(1);
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RSS polling iteration failed. Retrying in {RetryDelay}.", retryDelay);
                await Task.Delay(retryDelay, stoppingToken);
                retryDelay = TimeSpan.FromTicks(Math.Min(retryDelay.Ticks * 2, maxRetryDelay.Ticks));
            }
        }
    }
}
