using OcharoMarketing.Application.Scanning;
using OcharoMarketing.Application.Scanning.Commands;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Scanning.CommandHandlers;

/// <summary>Handles <see cref="SourceAnalysisCreateCommand"/>.</summary>
internal sealed class SourceAnalysisCreateCommandHandler(ISourceAnalysisService analyses)
    : ICommandHandler<SourceAnalysisCreateCommand, AppResult<SourceAnalysisModel>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<SourceAnalysisModel>> HandleAsync(
        SourceAnalysisCreateCommand request,
        CancellationToken ct)
    {
        var data = await analyses.AnalyzeAsync(request.Input, ct);

        return AppResult<SourceAnalysisModel>.Ok(data);
    }
}
