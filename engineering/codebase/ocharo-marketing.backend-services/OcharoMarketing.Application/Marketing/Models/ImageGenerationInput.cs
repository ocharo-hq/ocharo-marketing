namespace OcharoMarketing.Application.Marketing.Models;

/// <summary>Represents ImageGeneration data for the local marketing workflow.</summary>
public sealed record ImageGenerationInput(string Provider, string Prompt, string? ProductId = null, string[]? ReferenceAssetIds = null, string? Model = null);
