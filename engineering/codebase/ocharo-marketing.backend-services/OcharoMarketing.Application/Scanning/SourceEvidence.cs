namespace OcharoMarketing.Application.Scanning;

/// <summary>Identifies the immutable image used for a suggestion.</summary>
public sealed record SourceEvidence(string AssetId, string Sha256, string FileName, string Provenance);
