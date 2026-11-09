using OcharoBlog.Application.Blog.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Application.Blog.Queries;

/// <summary>Represents a query to get the catalogue products a story may feature.</summary>
public sealed record CatalogueProductGetAllQuery : IQuery<AppResult<IReadOnlyList<CatalogueProduct>>>;
