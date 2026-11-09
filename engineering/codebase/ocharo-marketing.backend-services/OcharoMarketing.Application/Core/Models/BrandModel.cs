namespace OcharoMarketing.Application.Core.Models;

/// <summary>Represents Brand data for the local marketing workflow.</summary>
public sealed record BrandModel(string Name, string Timezone, string? Handle);
