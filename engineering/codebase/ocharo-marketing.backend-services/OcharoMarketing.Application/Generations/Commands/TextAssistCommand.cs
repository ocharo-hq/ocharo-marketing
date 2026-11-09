using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Generations.Commands;

/// <summary>Represents a command that asks the text provider to assist with copy.</summary>
public sealed record TextAssistCommand : ICommand<AppResult<object>>
{
    /// <summary>Gets the operator's prompt.</summary>
    public required string Prompt { get; init; }

    /// <summary>Gets the assistance asked for.</summary>
    public required string Operation { get; init; }

    /// <summary>Gets the copy's language.</summary>
    public required string Locale { get; init; }
}
