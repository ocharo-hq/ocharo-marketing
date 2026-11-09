namespace OcharoMarketing.Application.Generations.Models;

/// <summary>Records who asked for a job: the work runs as the service, so it keeps their identifiers and never their
/// token.</summary>
public sealed record JobRequester(string Subject, string OrganizationId);
