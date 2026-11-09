using OcharoMarketing.Application.Automation.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Automation.Commands;

/// <summary>Represents a command that syncs products from Ocharo Catalogue now.</summary>
public sealed record CatalogueSyncCommand : ICommand<AppResult<CatalogueSyncReport>>
{
}
