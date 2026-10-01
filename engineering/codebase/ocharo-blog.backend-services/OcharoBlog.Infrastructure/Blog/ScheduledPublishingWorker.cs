using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OcharoBlog.Application.Blog.Services;

namespace OcharoBlog.Infrastructure.Blog;

/// <summary>Publishes scheduled stories when their time arrives; state lives in the database, so restarts lose nothing.</summary>
public sealed class ScheduledPublishingWorker(IServiceScopeFactory scopes, IOptions<BlogOptions> options, ILogger<ScheduledPublishingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(5, options.Value.SchedulerSeconds)));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var published = await scope.ServiceProvider.GetRequiredService<ArticleService>().PublishDueAsync(stoppingToken);
                foreach (var slug in published) logger.LogInformation("Published scheduled story {Slug}", slug);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Scheduled publishing pass failed; retrying on the next tick");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
