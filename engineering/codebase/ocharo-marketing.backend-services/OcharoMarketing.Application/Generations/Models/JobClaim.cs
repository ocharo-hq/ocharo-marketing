namespace OcharoMarketing.Application.Generations.Models;

/// <summary>Represents JobClaim data for the local marketing workflow.</summary>
public sealed record JobClaim(string Id, string Kind, string PayloadJson, string ClaimToken, int Attempt)
{
    /// <summary>Gets who asked for the job; null for a job from before they were recorded.</summary>
    public JobRequester? RequestedBy { get; init; }

    /// <summary>Gets when the job was asked for.</summary>
    public DateTimeOffset AcceptedAt { get; init; }
}
