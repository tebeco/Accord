using System;
using System.Threading;
using System.Threading.Tasks;
using Accord.Bot.Helpers;
using Accord.Services.Reminder;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;

namespace Accord.Bot.HostedServices;

public class RemindersHostedService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<RemindersHostedService> logger) : BackgroundService
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

                await ProcessReminders(mediator, channelApi, stoppingToken);
                retryDelay = TimeSpan.FromSeconds(1);
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Reminder polling iteration failed. Retrying in {RetryDelay}.", retryDelay);
                await Task.Delay(retryDelay, stoppingToken);
                retryDelay = TimeSpan.FromTicks(Math.Min(retryDelay.Ticks * 2, maxRetryDelay.Ticks));
            }
        }
    }

    private async Task ProcessReminders(IMediator mediator, 
        IDiscordRestChannelAPI channelApi, 
        CancellationToken stoppingToken)
    {
        var reminders = await mediator.Send(new GetRemindersToNotifyRequest(), stoppingToken);

        foreach (var reminder in reminders)
        {
            if (DateTime.Now - reminder.RemindAt < TimeSpan.FromMinutes(1))
            {
                var embed = new Embed
                {
                    Title = "Reminder",
                    Description = reminder.Message
                };

                await channelApi.CreateMessageAsync(new Snowflake(reminder.DiscordChannelId), DiscordFormatter.UserIdToMention(reminder.UserId), embeds: new[] { embed }, ct: stoppingToken);
            }

            await mediator.Send(new DeleteReminderRequest(reminder.UserId, reminder.Id), stoppingToken);
        }
    }
}
