namespace OcharoMarketing.Application.Scanning;

/// <summary>Records wording reviewed by an operator without changing catalogue facts.</summary>
public sealed record SourceReviewInput(int ExpectedRevision, string Text);
