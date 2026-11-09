using OcharoMarketing.Application.Inquiries;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Inquiries.Queries;

/// <summary>Represents a query for a page of an enquiry's activity.</summary>
public sealed record InquiryActivityGetQuery : IQuery<AppResult<InquiryActivityPage>>
{
    /// <summary>Gets the enquiry.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the page's first item.</summary>
    public required int Offset { get; init; }

    /// <summary>Gets the page's size.</summary>
    public required int Limit { get; init; }

    /// <summary>Gets the revision the page ends before; none starts at the newest.</summary>
    public int? BeforeRevision { get; init; }
}
