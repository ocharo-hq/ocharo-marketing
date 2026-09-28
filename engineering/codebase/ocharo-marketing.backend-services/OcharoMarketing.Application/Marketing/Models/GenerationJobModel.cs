namespace OcharoMarketing.Application.Marketing.Models;

/// <summary>Represents GenerationJob data for the local marketing workflow.</summary>
public sealed record GenerationJobModel(string Id, string Kind, string Provider, string Prompt, string? ProductId, string Status, DateTimeOffset CreatedAt, string[] OutputAssetIds, string? Message);
