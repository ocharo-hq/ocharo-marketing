namespace OcharoMarketing.Application.Inquiries;
public sealed record InquiryInput(string SubmissionId,string Name,string ContactMethod,string Contact,string Message,string SourcePath,string? ProductId,bool Consent,string? Website=null);
public sealed record InquiryRecord(string Id,string Fingerprint,string Name,string ContactMethod,string Contact,string Message,string SourcePath,string? ProductId,bool Consent,DateTimeOffset CreatedAt,string Status,int Revision,string? Note=null,DateTimeOffset? UpdatedAt=null,string? AssignedTo=null,DateTimeOffset? FollowUpAt=null,string? NextStep=null);
public sealed record InquiryFollowUp(string? AssignedTo,DateTimeOffset? FollowUpAt,string? NextStep);
public sealed record InquiryUpdate(int ExpectedRevision,string Status,string? Note,InquiryFollowUp? FollowUp=null);
public sealed record InquiryActivity(int Revision,DateTimeOffset RecordedAt,string Actor,string Status,string? Note,string? AssignedTo,DateTimeOffset? FollowUpAt,string? NextStep);
public sealed record InquiryPage(int Total,int Offset,int Limit,DateTimeOffset ObservedAt,IReadOnlyList<InquiryRecord> Items);
public sealed record InquiryActivityPage(int Total,int Offset,int Limit,IReadOnlyList<InquiryActivity> Items,int? NextBeforeRevision=null);
public interface IInquiryRepository
{
    Task<IReadOnlyList<InquiryRecord>> ListAsync(CancellationToken ct);
    Task<InquiryRecord?> FindAsync(string id,CancellationToken ct);
    Task InsertAsync(InquiryRecord record,CancellationToken ct);
    Task UpdateAsync(InquiryRecord record,int expectedRevision,InquiryActivity activity,CancellationToken ct);
    Task<InquiryActivityPage> ActivityAsync(string id,int offset,int limit,CancellationToken ct,int? beforeRevision=null);
}
public interface IPublishedProductVerifier { Task<bool> ExistsAsync(string productId,CancellationToken ct); }
