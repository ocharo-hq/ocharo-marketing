namespace OcharoMarketing.Application.Marketing.Models;

/// <summary>Represents Connection data for the local marketing workflow.</summary>
public sealed record ConnectionModel(string Id, string Name, string Kind, string Status, string[] Capabilities, string SetupHint, string DocumentationUrl,
    int ConnectionRevision = 0, string VerificationStatus = "not-verified", bool ExecutionEnabled = false, bool MetricReadsEnabled = false,
    string? AccountId = null, string? DestinationId = null, string? DisplayName = null, DateTimeOffset? VerifiedAt = null,
    string? SafeMessage = null, bool CanVerify = false);
