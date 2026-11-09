using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Inquiries.Commands;

/// <summary>Represents a command that records an operator's follow-up on an enquiry.</summary>
public sealed record InquiryUpdateCommand : ICommand<AppResult<InquiryRecord>>
{
    /// <summary>Gets the enquiry.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the follow-up, carrying the revision it was read at.</summary>
    public required InquiryUpdate Input { get; init; }

    /// <summary>Gets the signed-in operator; none refuses the update.</summary>
    public string? Actor { get; init; }
}
