using OcharoMarketing.Application.Inquiries;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Inquiries.Queries;

/// <summary>Represents a query for a page of enquiries.</summary>
public sealed record InquiriesGetQuery : IQuery<AppResult<InquiryPage>>
{
    /// <summary>Gets the status to keep; none keeps every status.</summary>
    public string? Status { get; init; }

    /// <summary>Gets the text to search for.</summary>
    public string? Search { get; init; }

    /// <summary>Gets the follow-up state to keep.</summary>
    public string? FollowUp { get; init; }

    /// <summary>Gets the page's first item.</summary>
    public required int Offset { get; init; }

    /// <summary>Gets the page's size.</summary>
    public required int Limit { get; init; }
}
