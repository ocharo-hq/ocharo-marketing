using OcharoMarketing.Application.Assets.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Assets.Queries;

/// <summary>Represents a query that opens an asset for the asset route.</summary>
public sealed record AssetGetQuery : IQuery<AppResult<AssetContent>>
{
    /// <summary>Gets the asset.</summary>
    public required string Id { get; init; }
}
