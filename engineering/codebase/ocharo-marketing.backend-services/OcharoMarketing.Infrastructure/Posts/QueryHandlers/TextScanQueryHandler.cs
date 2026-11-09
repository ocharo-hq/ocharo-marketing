using OcharoMarketing.Application.Posts.Models;
using OcharoMarketing.Application.Posts.Queries;
using OcharoMarketing.Infrastructure.Posts.Services;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Posts.QueryHandlers;

/// <summary>Handles <see cref="TextScanQuery"/>.</summary>
internal sealed class TextScanQueryHandler
    : IQueryHandler<TextScanQuery, AppResult<ScanModel>>
{
    /// <inheritdoc />
    public ValueTask<AppResult<ScanModel>> HandleAsync(TextScanQuery request, CancellationToken ct)
    {
        var data = MarketingService.Scan(request.Text);

        return ValueTask.FromResult(AppResult<ScanModel>.Ok(data));
    }
}
