using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Posts.Queries;

/// <summary>Represents a query for an approved draft revision's ZIP export.</summary>
public sealed record DraftExportGetQuery : IQuery<AppResult<byte[]>>
{
    /// <summary>Gets the draft.</summary>
    public required string Id { get; init; }
}
