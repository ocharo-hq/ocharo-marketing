namespace OcharoMarketing.Application.Marketing.Models;

/// <summary>Represents Asset data for the local marketing workflow.</summary>
public sealed record AssetModel(string Id, string FileName, string ContentType, long ByteLength, string Sha256, string Provenance, string Url, string Role);
