using OcharoMarketing.Application.Cards.Models;
using OcharoMarketing.Application.Generations.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Cards.Commands;

/// <summary>Represents a command that queues a card's render.</summary>
public sealed record CardRenderCommand : ICommand<AppResult<GenerationJobModel>>
{
    /// <summary>Gets the card's composition.</summary>
    public required CardInput Input { get; init; }

    /// <summary>Gets the signed-in person who asked, recorded on the job.</summary>
    public JobRequester? Requester { get; init; }
}
