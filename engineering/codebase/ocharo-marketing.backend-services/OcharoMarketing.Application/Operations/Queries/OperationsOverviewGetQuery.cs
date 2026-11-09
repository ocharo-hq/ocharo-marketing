using OcharoMarketing.Application.Operations.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Operations.Queries;

/// <summary>Represents a query for the read-only operations overview.</summary>
public sealed record OperationsOverviewGetQuery : IQuery<AppResult<OperationsOverviewModel>>
{
}
