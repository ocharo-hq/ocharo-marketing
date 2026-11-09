using OcharoBlog.Application.Blog.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Application.Blog.Queries;

/// <summary>Represents a query to get whether the Brand service supplies the site's logo and links.</summary>
public sealed record BrandStatusGetQuery : IQuery<AppResult<BrandStatus>>;
