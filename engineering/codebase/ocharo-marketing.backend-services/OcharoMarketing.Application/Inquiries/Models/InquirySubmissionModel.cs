namespace OcharoMarketing.Application.Inquiries.Models;

/// <summary>Represents a saved enquiry's reference.</summary>
/// <param name="Id">The enquiry's reference.</param>
/// <param name="Reused">Whether the submission repeated one already saved.</param>
public sealed record InquirySubmissionModel(string Id, bool Reused);
