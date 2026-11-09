namespace OcharoMarketing.Application.Deliveries.Models;

/// <summary>Defines safe durable delivery workflow data.</summary>
public sealed record PublicationClaim(string Id, string ClaimToken, PublicationOperationModel Operation);
