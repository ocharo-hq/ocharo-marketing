using OcharoBlog.Domain.Blog;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Application.Blog.Commands;

/// <summary>Represents a command to create or replace a topic.</summary>
public sealed record TopicSaveCommand : ICommand<AppResult<TopicDocument>>
{
    /// <summary>Gets the topic, keyed as its route names it.</summary>
    public required TopicDocument Topic { get; init; }
}
