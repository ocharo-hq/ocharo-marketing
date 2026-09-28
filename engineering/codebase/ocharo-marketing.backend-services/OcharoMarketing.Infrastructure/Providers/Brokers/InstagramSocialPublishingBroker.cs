using System.Net.Http.Json;
using OcharoMarketing.Application.Providers;

namespace OcharoMarketing.Infrastructure.Providers.Brokers;

/// <summary>Integrates Instagram professional-account image publication.</summary>
/// <remarks>Public JPEG hosting and durable container checkpoints are required; processing can remain pending.</remarks>
public sealed class InstagramSocialPublishingBroker(HttpClient client, InstagramProviderOptions options) : ISocialPublishingBroker
{
    public string Provider => "instagram";
    public IReadOnlySet<string> Capabilities { get; } = new HashSet<string> { "photo", "container-status" };

    public async Task<SocialPublishResult> PublishAsync(SocialPublishRequest input, CancellationToken cancellationToken)
    {
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.AccessToken) || !ProviderInput.IsGraphVersion(options.GraphVersion))
            return Result(BrokerOutcome.Rejected, "not_enabled", "Instagram requires explicit enablement, credentials, and a pinned Graph version.");
        if (!ProviderInput.IsNumericId(input.DestinationId) || input.Text.Length > 2200 || input.Template is not null)
            return Result(BrokerOutcome.Rejected, "invalid_input", "Instagram account or caption is invalid.");
        if (input.Media is not { Count: 1 })
            return Result(BrokerOutcome.Unsupported, "single_photo_required", "This adapter publishes one approved photo.");
        var photo = input.Media[0];
        if (photo.ContentType != "image/jpeg" || !ProviderInput.IsPublicHttps(photo.PublicUrl))
            return Result(BrokerOutcome.Unsupported, "public_jpeg_required", "Instagram publication requires a publicly reachable HTTPS JPEG.");
        if (input.SaveCheckpointAsync is null)
            return Result(BrokerOutcome.Rejected, "checkpoint_required", "Persist the Instagram container before publishing.");
        if (input.ResumeReceipt is not null && (input.ResumeReceipt.Provider != Provider || input.ResumeReceipt.Stage != "container_created"))
            return Result(BrokerOutcome.Unsupported, "reconcile_required", "Final Instagram receipts require reconciliation before another publication.");

        var baseUri = "https://" + (options.UseFacebookLogin ? "graph.facebook.com/" : "graph.instagram.com/") +
                      options.GraphVersion + "/";
        var checkpoint = input.ResumeReceipt;
        if (checkpoint is null)
        {
            using var create = ProviderHttpTransport.Create(HttpMethod.Post, baseUri + input.DestinationId + "/media", options.AccessToken);
            create.Content = JsonContent.Create(new { image_url = photo.PublicUrl, caption = input.Text });
            var created = await ProviderHttpTransport.SendAsync(client, create, true, cancellationToken);
            if (created.Outcome != BrokerOutcome.Succeeded) return Result(created.Outcome, created.Code, created.Message);
            if (created.Json is not { } createdRoot || ProviderHttpTransport.Id(createdRoot, "id") is not { } container)
                return Result(BrokerOutcome.Ambiguous, "missing_container", "Instagram accepted media without a usable container ID.");
            checkpoint = new(Provider, "container_created", container);
            try { await input.SaveCheckpointAsync(checkpoint, cancellationToken); }
            catch (Exception) { return new(BrokerOutcome.Ambiguous, Provider, "checkpoint_failed",
                "Container could not be checkpointed; final publication was not attempted.", checkpoint); }
        }

        using var status = ProviderHttpTransport.Create(HttpMethod.Get,
            baseUri + Uri.EscapeDataString(checkpoint.ExternalId) + "?fields=status_code,status", options.AccessToken);
        var readiness = await ProviderHttpTransport.SendAsync(client, status, false, cancellationToken);
        if (readiness.Outcome != BrokerOutcome.Succeeded || readiness.Json is not { } readyRoot)
            return new(readiness.Code is "http_400" or "http_401" or "http_403" ? BrokerOutcome.Rejected : BrokerOutcome.Pending,
                Provider, "status_unavailable", "Saved container status could not be read.", checkpoint);
        var state = ProviderHttpTransport.Text(readyRoot, "status_code");
        if (state == "PUBLISHED")
            return new(BrokerOutcome.Ambiguous, Provider, "already_published", "Container was published; recover its final media receipt.", checkpoint with { Stage = "publication_unknown" });
        if (state is "ERROR" or "EXPIRED")
            return new(BrokerOutcome.Rejected, Provider, "container_" + state.ToLowerInvariant(), "Instagram container cannot be published.", checkpoint);
        if (state != "FINISHED")
            return new(BrokerOutcome.Pending, Provider, "container_processing", "Instagram container is still processing.", checkpoint);

        try { await input.SaveCheckpointAsync(checkpoint with { Stage = "publication_started" }, cancellationToken); }
        catch (Exception) { return new(BrokerOutcome.Ambiguous, Provider, "checkpoint_failed",
            "Publication dispatch could not be checkpointed; final publication was not attempted.", checkpoint); }

        using var publish = ProviderHttpTransport.Create(HttpMethod.Post, baseUri + input.DestinationId + "/media_publish", options.AccessToken);
        publish.Content = JsonContent.Create(new { creation_id = checkpoint.ExternalId });
        var published = await ProviderHttpTransport.SendAsync(client, publish, true, cancellationToken);
        if (published.Outcome != BrokerOutcome.Succeeded)
            return new(published.Outcome, Provider, published.Code, published.Message,
                published.Outcome == BrokerOutcome.Ambiguous ? checkpoint with { Stage = "publication_unknown" } : checkpoint);
        if (published.Json is not { } root || ProviderHttpTransport.Id(root, "id") is not { } id)
            return new(BrokerOutcome.Ambiguous, Provider, "missing_receipt", "Instagram accepted publication without a usable media ID.", checkpoint with { Stage = "publication_unknown" });
        return new(BrokerOutcome.Succeeded, Provider, "published", "Instagram media published.",
            new(Provider, "published", id, new Dictionary<string, string> { ["container_id"] = checkpoint.ExternalId }));
    }

    private SocialPublishResult Result(BrokerOutcome outcome, string code, string message)
        => new(outcome, Provider, code, message);
}
