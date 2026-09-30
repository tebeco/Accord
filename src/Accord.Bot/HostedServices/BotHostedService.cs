using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Accord.Bot.HostedServices;

public class BotHostedService(
    BotClient botClient,
    ILogger<BotHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var retryDelay = TimeSpan.FromSeconds(1);
        var maxRetryDelay = TimeSpan.FromSeconds(60);

        while (!stoppingToken.IsCancellationRequested)
        {
            var attemptStartedAt = DateTimeOffset.UtcNow;
            try
            {
                await botClient.Run(stoppingToken);
                logger.LogWarning("Discord gateway stopped. Reconnecting in {RetryDelay}.", retryDelay);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Discord gateway failed. Reconnecting in {RetryDelay}.", retryDelay);
            }

            if (DateTimeOffset.UtcNow - attemptStartedAt > TimeSpan.FromMinutes(5))
            {
                retryDelay = TimeSpan.FromSeconds(1);
            }

            try
            {
                await Task.Delay(retryDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            retryDelay = TimeSpan.FromTicks(Math.Min(retryDelay.Ticks * 2, maxRetryDelay.Ticks));
        }
    }
}
