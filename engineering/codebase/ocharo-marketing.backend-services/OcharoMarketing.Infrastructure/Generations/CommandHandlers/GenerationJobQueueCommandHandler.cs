using OcharoMarketing.Application.Assets.Services;
using OcharoMarketing.Application.Core;
using OcharoMarketing.Application.Core.Errors;
using OcharoMarketing.Application.Core.Repositories;
using OcharoMarketing.Application.Generations.Commands;
using OcharoMarketing.Application.Generations.Models;
using OcharoMarketing.Application.Generations.Services;
using WoW.Two.Sdk.Beta.Be.Foundation.Errors;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Generations.CommandHandlers;

/// <summary>Handles <see cref="GenerationJobQueueCommand"/>: refuses a disabled provider, an oversized prompt, an
/// unknown product or too many references, checks each reference, then queues the job.</summary>
internal sealed class GenerationJobQueueCommandHandler(
    IMarketingRepository repository,
    IAssetService assets,
    IExternalMarketingService external,
    TimeProvider time)
    : ICommandHandler<GenerationJobQueueCommand, AppResult<GenerationJobModel>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<GenerationJobModel>> HandleAsync(
        GenerationJobQueueCommand request,
        CancellationToken ct)
    {
        var input = request.Input;
        if (!external.CanGenerate(input.Provider))
            return Fail(MarketingErrors.Conflict("This image provider is not enabled and configured."));
        if (string.IsNullOrWhiteSpace(input.Prompt) || input.Prompt.Length > 12000)
            return Fail(MarketingErrors.Invalid("Provide a generation prompt under 12000 characters."));
        if (input.ProductId is not null && await repository.FindProductAsync(input.ProductId, ct) is null)
            return Fail(MarketingErrors.NotFound("Product not found."));
        if ((input.ReferenceAssetIds?.Length ?? 0) > 5)
            return Fail(MarketingErrors.Invalid("Select at most five reference images."));
        foreach (var reference in input.ReferenceAssetIds ?? [])
            await assets.ReadAsync(reference, ct);

        var job = new GenerationJobModel(
            Guid.NewGuid().ToString("N"),
            "image",
            input.Provider,
            input.Prompt,
            input.ProductId,
            "queued",
            time.GetUtcNow(),
            [],
            null);
        await repository.EnqueueAsync(
            job,
            MarketingJson.Write(input),
            request.Requester,
            ct);

        return AppResult<GenerationJobModel>.Ok(job);
    }

    private static AppResult<GenerationJobModel> Fail(AppError error)
    {
        return AppResult<GenerationJobModel>.Fail(error);
    }
}
