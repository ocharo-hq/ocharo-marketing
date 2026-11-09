using OcharoMarketing.Application.Scanning;
using OcharoMarketing.Application.Scanning.Commands;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Scanning.CommandHandlers;

/// <summary>Handles <see cref="SourceAnalysisReviewCommand"/>.</summary>
internal sealed class SourceAnalysisReviewCommandHandler(ISourceAnalysisService analyses)
    : ICommandHandler<SourceAnalysisReviewCommand, AppResult<SourceAnalysisModel>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<SourceAnalysisModel>> HandleAsync(
        SourceAnalysisReviewCommand request,
        CancellationToken ct)
    {
        var data = await analyses.ReviewAsync(request.Id, request.Input, ct);

        return AppResult<SourceAnalysisModel>.Ok(data);
    }
}
