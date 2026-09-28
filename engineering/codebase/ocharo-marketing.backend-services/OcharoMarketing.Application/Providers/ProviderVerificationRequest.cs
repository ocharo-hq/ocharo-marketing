namespace OcharoMarketing.Application.Providers;

/// <summary>Identifies a requested destination and optional expected source account.</summary>
public sealed record ProviderVerificationRequest(string DestinationId, string? ExpectedAccountId = null);
