namespace OcharoMarketing.Api.Access;

/// <summary>The organization this Marketing instance serves (section <c>Marketing</c>): only its members reach the
/// private routes.</summary>
public sealed class MarketingAccessOptions
{
    public const string Section = "Marketing";

    /// <summary>The Ocharo organization's id at the Identity instance (<c>Marketing:OrganizationId</c>).
    /// Required.</summary>
    public Guid OrganizationId { get; set; }
}
