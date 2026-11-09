using OcharoBlog.Application.Blog.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Application.Blog.Commands;

/// <summary>Represents a command to import the editorial research pack as drafts.</summary>
public sealed record ResearchImportCommand : ICommand<AppResult<StarterImportResult>>
{
    /// <summary>Gets the host's content root, which holds the pack.</summary>
    public required string ContentRoot { get; init; }
}
