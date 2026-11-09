using OcharoMarketing.Application.Core.Errors;
using OcharoMarketing.Application.Inquiries;
using OcharoMarketing.Application.Inquiries.Commands;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Inquiries.CommandHandlers;

/// <summary>Handles <see cref="InquiryUpdateCommand"/>: refuses an update without a signed-in operator.</summary>
internal sealed class InquiryUpdateCommandHandler(InquiryService inquiries)
    : ICommandHandler<InquiryUpdateCommand, AppResult<InquiryRecord>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<InquiryRecord>> HandleAsync(InquiryUpdateCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Actor))
        {
            var refusal = MarketingErrors.Unauthenticated("A current operator identity is required.");
            return AppResult<InquiryRecord>.Fail(refusal);
        }

        var inquiry = await inquiries.UpdateAsync(
            request.Id,
            request.Input,
            ct,
            request.Actor);

        return AppResult<InquiryRecord>.Ok(inquiry);
    }
}
