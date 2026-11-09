using OcharoMarketing.Application.Assets.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Assets.Commands;

/// <summary>Represents a command that stores an uploaded image as an immutable asset.</summary>
public sealed record AssetSaveCommand : ICommand<AppResult<AssetModel>>
{
    /// <summary>The largest upload accepted, in bytes (25 MB).</summary>
    public const int MaximumBytes = 26_214_400;

    /// <summary>Gets the uploaded bytes.</summary>
    public required byte[] Bytes { get; init; }

    /// <summary>Gets the uploaded file's name.</summary>
    public required string FileName { get; init; }

    /// <summary>Gets where the image came from.</summary>
    public required string Provenance { get; init; }

    /// <summary>Gets the asset's role: a reference, a product image, a card.</summary>
    public required string Role { get; init; }
}
