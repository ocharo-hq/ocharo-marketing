using OcharoBlog.Domain.Blog;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Application.Blog.Queries;

/// <summary>Represents a query to get every topic in reading order.</summary>
public sealed record TopicGetAllQuery : IQuery<AppResult<IReadOnlyList<TopicDocument>>>;
