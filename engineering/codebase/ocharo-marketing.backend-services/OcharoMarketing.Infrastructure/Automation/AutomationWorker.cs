using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OcharoMarketing.Infrastructure.Automation;

/// <summary>Checks every five minutes whether unattended catalogue sync or slot planning is due (both off by default).</summary>
public sealed class AutomationWorker(IServiceScopeFactory scopes, ILogger<AutomationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                if (await scope.ServiceProvider.GetRequiredService<AutomationRunner>().RunDueAsync(stoppingToken))
                    logger.LogInformation("Unattended marketing automation pass completed");
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Unattended automation pass failed; retrying on the next tick");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
