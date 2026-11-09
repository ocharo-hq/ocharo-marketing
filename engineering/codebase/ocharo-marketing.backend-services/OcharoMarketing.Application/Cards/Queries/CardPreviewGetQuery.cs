using OcharoMarketing.Application.Cards.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Cards.Queries;

/// <summary>Represents a query for a card's preview, composed from a product's images.</summary>
public sealed record CardPreviewGetQuery : IQuery<AppResult<CardPreview>>
{
    /// <summary>Gets the card's composition.</summary>
    public required CardInput Input { get; init; }
}
