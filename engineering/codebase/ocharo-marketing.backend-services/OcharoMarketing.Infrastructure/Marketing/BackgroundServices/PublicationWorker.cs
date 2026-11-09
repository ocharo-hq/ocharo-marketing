using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OcharoMarketing.Infrastructure.Marketing.Services;

namespace OcharoMarketing.Infrastructure.Marketing.BackgroundServices;

/// <summary>Processes only explicitly authorized durable publication operations.</summary>
public sealed class PublicationWorker(
    IServiceScopeFactory scopes,
    ILogger<PublicationWorker> logger,
    WorkerHealthService health,
    SqliteImportGate importGate) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        health.Started("publication");
        var waiting = false;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Until import-sqlite copies the earlier store, recovery and claims would write into the schema it must
                // find empty.
                if (importGate.IsPending)
                {
                    if (!waiting)
                        logger.LogInformation(
                            "Publications wait for import-sqlite to import {Store}.", importGate.SourcePath);
                    waiting = true;
                    await Task.Delay(SqliteImportGate.CheckEvery, stoppingToken);
                    continue;
                }
                waiting = false;
                using var scope = scopes.CreateScope();
                var worked = await scope.ServiceProvider.GetRequiredService<PublicationService>().ProcessNextAsync(stoppingToken);
                health.Completed("publication");
                if (!worked) await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception exception)
            {
                health.Failed("publication");
                logger.LogWarning("Publication worker paused after {FailureType}; durable state will determine recovery.", exception.GetType().Name);
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }
}
