using OcharoMarketing.Application.Core.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Core.Queries;

/// <summary>Represents a query for the whole marketing workspace.</summary>
public sealed record WorkspaceGetQuery : IQuery<AppResult<WorkspaceModel>>;
