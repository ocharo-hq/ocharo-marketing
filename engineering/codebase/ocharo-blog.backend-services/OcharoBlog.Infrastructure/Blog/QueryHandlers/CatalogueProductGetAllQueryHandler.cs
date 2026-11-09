using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Queries;
using OcharoBlog.Application.Blog.Services;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.QueryHandlers;

/// <summary>Handles <see cref="CatalogueProductGetAllQuery"/>.</summary>
internal sealed class CatalogueProductGetAllQueryHandler(ICatalogueFeed catalogue)
    : IQueryHandler<CatalogueProductGetAllQuery, AppResult<IReadOnlyList<CatalogueProduct>>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<IReadOnlyList<CatalogueProduct>>> HandleAsync(
        CatalogueProductGetAllQuery request,
        CancellationToken ct)
    {
        return AppResult<IReadOnlyList<CatalogueProduct>>.Ok(await catalogue.GetProductsAsync(ct));
    }
}
