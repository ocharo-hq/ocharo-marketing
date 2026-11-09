using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Queries;
using OcharoBlog.Application.Blog.Services;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.QueryHandlers;

/// <summary>Handles <see cref="BrandStatusGetQuery"/>.</summary>
internal sealed class BrandStatusGetQueryHandler(IBrandKit brand)
    : IQueryHandler<BrandStatusGetQuery, AppResult<BrandStatus>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<BrandStatus>> HandleAsync(BrandStatusGetQuery request, CancellationToken ct)
    {
        var status = await brand.GetAsync(ct) is { } kit
            ? new BrandStatus(true, kit.Release, kit.LogoSha256, kit.Links)
            : new BrandStatus(false, 0, null, []);

        return AppResult<BrandStatus>.Ok(status);
    }
}
