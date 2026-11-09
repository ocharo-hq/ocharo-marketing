using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OcharoMarketing.Application.Cards;
using OcharoMarketing.Application.Cards.Services;
using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Application.Marketing.Repositories;
using OcharoMarketing.Application.Marketing.Services;
using OcharoMarketing.Infrastructure.Marketing.Services;
using WoW.Two.Sdk.Beta.Be.Jobs.Core.PersonOperations;

namespace OcharoMarketing.Infrastructure.Marketing.BackgroundServices;

/// <summary>Processes durable local jobs with a fresh dependency scope and conservative external recovery.</summary>
public sealed class GenerationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<GenerationWorker> logger,
    WorkerHealthService health,
    IPersonOperationTracker operations,
    TimeProvider time) : BackgroundService
{
    private const string AccessLostMessage = "This job stopped because the person who asked for it no longer has access.";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        health.Started("generation");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IMarketingRepository>();
                var claim = await repository.ClaimAsync(time.GetUtcNow(), TimeSpan.FromMinutes(15), stoppingToken);
                health.Completed("generation");
                if (claim is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }
                using var lease = Begin(claim, stoppingToken);
                var jobToken = lease?.CancellationToken ?? stoppingToken;
                var started = false;
                try
                {
                    jobToken.ThrowIfCancellationRequested();
                    started = true;
                    if (claim.Kind == "card")
                    {
                        var payload = MarketingJson.Read<CardRenderPayload>(claim.PayloadJson);
                        var cards = scope.ServiceProvider.GetRequiredService<ICardRenderingService>();
                        var assets = scope.ServiceProvider.GetRequiredService<IAssetService>();
                        var bytes = await cards.RenderPngAsync(payload.Html, jobToken);
                        var image = await assets.SaveAsync(bytes, $"{payload.PresetId}-{claim.Id}.png",
                            $"Deterministic card render of frozen template HTML; job {claim.Id}.", "card", jobToken, $"card-{claim.Id}");
                        await repository.CompleteAsync(claim, [image.Id], stoppingToken);
                    }
                    else
                    {
                        await scope.ServiceProvider.GetRequiredService<IExternalMarketingService>().ExecuteGenerationAsync(claim, jobToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Preserve running state. Lease recovery distinguishes deterministic work from external effects.
                    return;
                }
                catch (OperationCanceledException) when (lease?.CancelReason is not null)
                {
                    // An external call already under way may have reached the provider, so it stays for a person to inspect.
                    logger.LogInformation("Marketing job {JobId} stopped: {Reason}.", claim.Id, lease.CancelReason);
                    await repository.FailAsync(claim, AccessLostMessage, started && claim.Kind != "card", stoppingToken);
                }
                catch (Exception exception)
                {
                    health.Failed("generation");
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
                health.Failed("generation");
                    logger.LogWarning("Marketing worker paused after {FailureType}.", exception.GetType().Name);
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }

    /// <summary>Declares a job someone asked for, with the defaults: signing out leaves it running, and losing access stops it,
    /// including while it waited in the queue.</summary>
    private PersonOperationLease? Begin(JobClaim claim, CancellationToken stoppingToken) =>
        claim.RequestedBy is not { } person ? null : operations.Begin(new PersonOperationDeclaration
        {
            Name = $"marketing-{claim.Kind}",
            Subject = person.Subject,
            OrganizationId = person.OrganizationId,
            AcceptedAt = claim.AcceptedAt,
        }, stoppingToken);
}
