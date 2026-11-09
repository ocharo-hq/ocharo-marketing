using OcharoMarketing.Application.Scanning;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Scanning.Commands;

/// <summary>Represents a command that saves an operator's review of a source analysis.</summary>
public sealed record SourceAnalysisReviewCommand : ICommand<AppResult<SourceAnalysisModel>>
{
    /// <summary>Gets the analysis.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the reviewed text, carrying the revision it was read at.</summary>
    public required SourceReviewInput Input { get; init; }
}
