using OcharoMarketing.Application.Inquiries;
using OcharoMarketing.Application.Inquiries.Queries;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Inquiries.QueryHandlers;

/// <summary>Handles <see cref="InquiriesGetQuery"/>.</summary>
internal sealed class InquiriesGetQueryHandler(InquiryService inquiries)
    : IQueryHandler<InquiriesGetQuery, AppResult<InquiryPage>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<InquiryPage>> HandleAsync(InquiriesGetQuery request, CancellationToken ct)
    {
        var data = await inquiries.ListAsync(
            request.Status,
            request.Search,
            request.FollowUp,
            request.Offset,
            request.Limit,
            ct);

        return AppResult<InquiryPage>.Ok(data);
    }
}
