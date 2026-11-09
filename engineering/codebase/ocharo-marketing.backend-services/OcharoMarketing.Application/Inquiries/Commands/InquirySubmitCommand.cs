using OcharoMarketing.Application.Inquiries.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Inquiries.Commands;

/// <summary>Represents a command that saves an enquiry from the public form.</summary>
public sealed record InquirySubmitCommand : ICommand<AppResult<InquirySubmissionModel>>
{
    /// <summary>Gets the enquiry.</summary>
    public required InquiryInput Input { get; init; }
}
