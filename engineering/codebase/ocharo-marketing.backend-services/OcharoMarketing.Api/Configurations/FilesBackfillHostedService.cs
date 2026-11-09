using OcharoMarketing.Infrastructure.Marketing.Services;

namespace OcharoMarketing.Api.Configurations;

/// <summary>Fills, at boot, the file ids of the assets and exports the migration moved into WoW2 Files; an unreachable Files
/// leaves them on their local bytes, never the boot. Runs only when <c>Files:BaseUrl</c> is set, after the migrations.</summary>
public sealed class FilesBackfillHostedService(IServiceProvider services, ILogger<FilesBackfillHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        if (!HostConfiguration.UsesFiles(scope.ServiceProvider)) return;
        try
        {
            await scope.ServiceProvider.GetRequiredService<FilesAssetBackfill>().RunAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            logger.LogWarning(exception, "Marketing's file ids stay unfilled: WoW2 Files could not be reached at boot.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
