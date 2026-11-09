using OcharoMarketing.Application.Generations.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Generations.Commands;

/// <summary>Represents a command that queues an image generation job.</summary>
public sealed record GenerationJobQueueCommand : ICommand<AppResult<GenerationJobModel>>
{
    /// <summary>Gets the provider, prompt, product and reference images to generate from.</summary>
    public required ImageGenerationInput Input { get; init; }

    /// <summary>Gets the signed-in person who asked, recorded on the job.</summary>
    public JobRequester? Requester { get; init; }
}
