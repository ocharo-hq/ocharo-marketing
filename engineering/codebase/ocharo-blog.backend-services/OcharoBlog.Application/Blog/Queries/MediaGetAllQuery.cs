using OcharoBlog.Domain.Blog;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Application.Blog.Queries;

/// <summary>Represents a query to get every stored image, newest first.</summary>
public sealed record MediaGetAllQuery : IQuery<AppResult<IReadOnlyList<MediaBlobRecord>>>;
