using OcharoMarketing.Application.Posts.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Posts.Queries;

/// <summary>Represents a query that scans pasted source text into reviewable suggestions.</summary>
public sealed record TextScanQuery : IQuery<AppResult<ScanModel>>
{
    /// <summary>Gets the pasted source text.</summary>
    public required string Text { get; init; }
}
