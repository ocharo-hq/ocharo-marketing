using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OcharoMarketing.Infrastructure.Marketing.Services;

namespace OcharoMarketing.Infrastructure.Automation;

/// <summary>Checks every five minutes whether unattended catalogue sync or slot planning is due (both off by default).</summary>
public sealed class AutomationWorker(IServiceScopeFactory scopes, ILogger<AutomationWorker> logger, WorkerHealthService health) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        health.Started("automation");
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                if (await scope.ServiceProvider.GetRequiredService<AutomationRunner>().RunDueAsync(stoppingToken))
                    logger.LogInformation("Unattended marketing automation pass completed");
                health.Completed("automation");
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                health.Failed("automation");
                logger.LogError(exception, "Unattended automation pass failed; retrying on the next tick");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
