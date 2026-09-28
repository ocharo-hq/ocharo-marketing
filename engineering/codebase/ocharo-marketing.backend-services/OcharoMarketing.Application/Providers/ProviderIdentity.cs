namespace OcharoMarketing.Application.Providers;

/// <summary>Carries safe numeric identity metadata observed by a provider read, never credentials.</summary>
public sealed record ProviderIdentity(
    string AccountId, string DestinationId, string DisplayName,
    IReadOnlyList<string> Capabilities, DateTimeOffset VerifiedAt);
