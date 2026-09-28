namespace OcharoMarketing.Application.Marketing.Models;

/// <summary>Represents JobClaim data for the local marketing workflow.</summary>
public sealed record JobClaim(string Id, string Kind, string PayloadJson, string ClaimToken, int Attempt);
