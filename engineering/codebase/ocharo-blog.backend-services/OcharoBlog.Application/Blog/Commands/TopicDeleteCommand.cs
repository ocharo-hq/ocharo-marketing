using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Application.Blog.Commands;

/// <summary>Represents a command to delete a topic no story uses.</summary>
public sealed record TopicDeleteCommand : ICommand<AppResult<string>>
{
    /// <summary>Gets the topic's key.</summary>
    public required string Key { get; init; }
}
