using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OcharoMarketing.Infrastructure.Marketing.Services;

namespace OcharoMarketing.Infrastructure.Marketing.BackgroundServices;

/// <summary>Processes only explicitly authorized durable publication operations.</summary>
public sealed class PublicationWorker(IServiceScopeFactory scopes, ILogger<PublicationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var worked = await scope.ServiceProvider.GetRequiredService<PublicationService>().ProcessNextAsync(stoppingToken);
                if (!worked) await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception exception)
            {
                logger.LogWarning("Publication worker paused after {FailureType}; durable state will determine recovery.", exception.GetType().Name);
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }
}
