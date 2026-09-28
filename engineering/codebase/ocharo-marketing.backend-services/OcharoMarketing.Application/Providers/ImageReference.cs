namespace OcharoMarketing.Application.Providers;

/// <summary>Carries an image without exposing a filesystem path or credential.</summary>
public sealed record ImageReference(byte[] Data, string ContentType, string FileName = "reference.png");
