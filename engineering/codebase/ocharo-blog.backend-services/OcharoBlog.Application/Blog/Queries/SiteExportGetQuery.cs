using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Application.Blog.Queries;

/// <summary>Represents a query to get a static copy of the published site as a zip archive.</summary>
public sealed record SiteExportGetQuery : IQuery<AppResult<byte[]>>;
