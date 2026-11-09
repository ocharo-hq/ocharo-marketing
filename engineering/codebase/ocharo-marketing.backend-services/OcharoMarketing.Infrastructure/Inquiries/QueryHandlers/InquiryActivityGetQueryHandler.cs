using OcharoMarketing.Application.Inquiries;
using OcharoMarketing.Application.Inquiries.Queries;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Inquiries.QueryHandlers;

/// <summary>Handles <see cref="InquiryActivityGetQuery"/>.</summary>
internal sealed class InquiryActivityGetQueryHandler(InquiryService inquiries)
    : IQueryHandler<InquiryActivityGetQuery, AppResult<InquiryActivityPage>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<InquiryActivityPage>> HandleAsync(
        InquiryActivityGetQuery request,
        CancellationToken ct)
    {
        var data = await inquiries.ActivityAsync(
            request.Id,
            request.Offset,
            request.Limit,
            ct,
            request.BeforeRevision);

        return AppResult<InquiryActivityPage>.Ok(data);
    }
}
