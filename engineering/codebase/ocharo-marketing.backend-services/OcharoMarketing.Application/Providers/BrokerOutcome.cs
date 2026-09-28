namespace OcharoMarketing.Application.Providers;

/// <summary>Describes evidence obtained from an external operation.</summary>
public enum BrokerOutcome
{
    Succeeded,
    Pending,
    Rejected,
    Unsupported,
    Ambiguous
}
