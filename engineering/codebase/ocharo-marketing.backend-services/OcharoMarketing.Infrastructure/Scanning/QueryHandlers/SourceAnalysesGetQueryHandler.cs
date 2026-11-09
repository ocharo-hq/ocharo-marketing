using OcharoMarketing.Application.Scanning;
using OcharoMarketing.Application.Scanning.Queries;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Scanning.QueryHandlers;

/// <summary>Handles <see cref="SourceAnalysesGetQuery"/>.</summary>
internal sealed class SourceAnalysesGetQueryHandler(ISourceAnalysisService analyses)
    : IQueryHandler<SourceAnalysesGetQuery, AppResult<IReadOnlyList<SourceAnalysisModel>>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<IReadOnlyList<SourceAnalysisModel>>> HandleAsync(
        SourceAnalysesGetQuery request,
        CancellationToken ct)
    {
        var data = await analyses.ListAsync(ct);

        return AppResult<IReadOnlyList<SourceAnalysisModel>>.Ok(data);
    }
}
