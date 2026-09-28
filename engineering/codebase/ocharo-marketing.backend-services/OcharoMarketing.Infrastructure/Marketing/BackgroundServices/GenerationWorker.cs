using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OcharoMarketing.Application.Cards;
using OcharoMarketing.Application.Cards.Services;
using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Application.Marketing.Repositories;
using OcharoMarketing.Application.Marketing.Services;
using OcharoMarketing.Infrastructure.Marketing.Services;

namespace OcharoMarketing.Infrastructure.Marketing.BackgroundServices;

/// <summary>Processes durable local jobs with a fresh dependency scope and conservative external recovery.</summary>
public sealed class GenerationWorker(IServiceScopeFactory scopeFactory, ILogger<GenerationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IMarketingRepository>();
                var claim = await repository.ClaimAsync(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(15), stoppingToken);
                if (claim is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }
                try
                {
                    if (claim.Kind == "card")
                    {
                        var payload = MarketingJson.Read<CardRenderPayload>(claim.PayloadJson);
                        var cards = scope.ServiceProvider.GetRequiredService<ICardRenderingService>();
                        var assets = scope.ServiceProvider.GetRequiredService<IAssetService>();
                        var bytes = await cards.RenderPngAsync(payload.Html, stoppingToken);
                        var image = await assets.SaveAsync(bytes, $"{payload.PresetId}-{claim.Id}.png",
                            $"Deterministic card render of frozen template HTML; job {claim.Id}.", "card", stoppingToken, $"card-{claim.Id}");
                        await repository.CompleteAsync(claim, [image.Id], stoppingToken);
                    }
                    else
                    {
                        await scope.ServiceProvider.GetRequiredService<IExternalMarketingService>().ExecuteGenerationAsync(claim, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Preserve running state. Lease recovery distinguishes deterministic work from external effects.
                    return;
                }
                catch (Exception exception)
                {
                    logger.LogWarning("Marketing job {JobId} failed with {FailureType}.", claim.Id, exception.GetType().Name);
                    var safeMessage = claim.Kind == "card"
                        ? exception is CardRenderingException controlled ? controlled.Message
                            : "Local rendering failed. Check renderer setup and retry the card."
                        : "External execution did not finish reliably. Inspect provider history before retrying.";
                    await repository.FailAsync(claim, safeMessage, claim.Kind != "card", stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (Exception exception)
            {
                logger.LogWarning("Marketing worker paused after {FailureType}.", exception.GetType().Name);
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }
}
