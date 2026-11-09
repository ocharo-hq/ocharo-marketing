using OcharoMarketing.Application.Inquiries;
using OcharoMarketing.Application.Inquiries.Commands;
using OcharoMarketing.Application.Inquiries.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Inquiries.CommandHandlers;

/// <summary>Handles <see cref="InquirySubmitCommand"/>.</summary>
internal sealed class InquirySubmitCommandHandler(InquiryService inquiries)
    : ICommandHandler<InquirySubmitCommand, AppResult<InquirySubmissionModel>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<InquirySubmissionModel>> HandleAsync(
        InquirySubmitCommand request,
        CancellationToken ct)
    {
        var (id, reused) = await inquiries.SubmitAsync(request.Input, ct);

        return AppResult<InquirySubmissionModel>.Ok(new InquirySubmissionModel(id, reused));
    }
}
