using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Automation.Queries;

/// <summary>Represents a query for a product's caption from its template.</summary>
public sealed record CaptionGetQuery : IQuery<AppResult<string>>
{
    /// <summary>Gets the product.</summary>
    public required string ProductId { get; init; }

    /// <summary>Gets the caption's language.</summary>
    public required string Locale { get; init; }
}
