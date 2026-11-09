using OcharoMarketing.Application.Automation.Queries;
using OcharoMarketing.Infrastructure.Automation;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Automation.QueryHandlers;

/// <summary>Handles <see cref="CaptionGetQuery"/>.</summary>
internal sealed class CaptionGetQueryHandler(AutomationService automation)
    : IQueryHandler<CaptionGetQuery, AppResult<string>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<string>> HandleAsync(CaptionGetQuery request, CancellationToken ct)
    {
        var data = await automation.CaptionAsync(request.ProductId, request.Locale, ct);

        return AppResult<string>.Ok(data);
    }
}
