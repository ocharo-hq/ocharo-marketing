using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Queries;
using OcharoBlog.Application.Blog.Services;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.QueryHandlers;

/// <summary>Handles <see cref="SiteSettingsGetQuery"/>.</summary>
internal sealed class SiteSettingsGetQueryHandler(SiteSettingsService settings)
    : IQueryHandler<SiteSettingsGetQuery, AppResult<SettingsEnvelope>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<SettingsEnvelope>> HandleAsync(SiteSettingsGetQuery request, CancellationToken ct)
    {
        return AppResult<SettingsEnvelope>.Ok(await settings.GetAsync(ct));
    }
}
