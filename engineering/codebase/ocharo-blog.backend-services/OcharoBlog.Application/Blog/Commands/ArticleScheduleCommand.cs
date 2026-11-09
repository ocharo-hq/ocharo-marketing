using OcharoBlog.Application.Blog.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Application.Blog.Commands;

/// <summary>Represents a command to schedule a story for publication.</summary>
public sealed record ArticleScheduleCommand : ICommand<AppResult<ArticleDetail>>
{
    /// <summary>Gets the story's id.</summary>
    public required Guid Id { get; init; }

    /// <summary>Gets the revision the editor acted on.</summary>
    public required int ExpectedRevision { get; init; }

    /// <summary>Gets when the story goes live; none when the editor chose no time.</summary>
    public DateTimeOffset? PublishAt { get; init; }
}
