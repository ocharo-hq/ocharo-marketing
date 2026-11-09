using OcharoMarketing.Application.Automation.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Automation.Queries;

/// <summary>Represents a query for the published Ocharo Blog stories.</summary>
public sealed record BlogStoriesGetQuery : IQuery<AppResult<BlogStoryModel[]>>
{
}
