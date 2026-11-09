using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Cards.Queries;

/// <summary>Represents a query for a PDF atlas of completed card renders.</summary>
public sealed record CardAtlasGetQuery : IQuery<AppResult<byte[]>>
{
    /// <summary>Gets the card renders to lay out.</summary>
    public required string[] AssetIds { get; init; }
}
