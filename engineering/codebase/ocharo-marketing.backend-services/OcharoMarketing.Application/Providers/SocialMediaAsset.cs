namespace OcharoMarketing.Application.Providers;

/// <summary>Carries selected publication media and an optional provider-fetchable URL.</summary>
public sealed record SocialMediaAsset(
    byte[] Data,
    string ContentType,
    string FileName = "card.png",
    string? PublicUrl = null);
