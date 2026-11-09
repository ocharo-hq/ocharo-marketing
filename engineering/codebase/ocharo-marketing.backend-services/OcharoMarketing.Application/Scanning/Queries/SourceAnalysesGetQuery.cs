using OcharoMarketing.Application.Scanning;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Scanning.Queries;

/// <summary>Represents a query for every saved source analysis.</summary>
public sealed record SourceAnalysesGetQuery : IQuery<AppResult<IReadOnlyList<SourceAnalysisModel>>>
{
}
