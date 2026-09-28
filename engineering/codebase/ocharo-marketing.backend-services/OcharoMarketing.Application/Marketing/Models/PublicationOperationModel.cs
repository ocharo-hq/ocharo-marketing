using OcharoMarketing.Application.Providers;

namespace OcharoMarketing.Application.Marketing.Models;

/// <summary>Defines safe durable delivery workflow data.</summary>
public sealed record PublicationOperationModel(string Id, string ScheduleId, string Provider, string ConnectionId, int ConnectionRevision,
    string Status, string Phase, DateTimeOffset DueAt, DateTimeOffset AuthorizedAt, DateTimeOffset? DispatchStartedAt, DateTimeOffset UpdatedAt,
    string? Message, ProviderReceipt? Receipt, PublicationSnapshotModel Snapshot)
{
    public bool CanCancel => Status is "queued" or "held" || Status == "running" && DispatchStartedAt is null;
    public bool CanReconcile => Status == "unknown" && Provider == "x" && Receipt is { Provider: "x" }
        && (Receipt.Stage == "published" || Receipt.Stage == "publication_unknown" && Receipt.RelatedIds?.GetValueOrDefault("post_id") == Receipt.ExternalId);
    public string ReconciliationMessage => CanReconcile ? "An explicit read-only X receipt check is available; provider credits and the configured read budget apply." : Status == "unknown"
        ? "Inspect the provider account history. No safe automatic receipt lookup is available; this operation will not repost."
        : "Receipt details are available without contacting the provider.";
}
