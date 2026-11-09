using OcharoMarketing.Application.Scanning;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Scanning.Commands;

/// <summary>Represents a command that analyses a source image into reviewable suggestions.</summary>
public sealed record SourceAnalysisCreateCommand : ICommand<AppResult<SourceAnalysisModel>>
{
    /// <summary>Gets the source and the prompt.</summary>
    public required SourceAnalysisInput Input { get; init; }
}
