namespace OcharoMarketing.Application.Marketing.Models;

/// <summary>Defines safe durable delivery workflow data.</summary>
public sealed record PublicationClaim(string Id, string ClaimToken, PublicationOperationModel Operation);
