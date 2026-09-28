namespace OcharoMarketing.Application.Providers;

/// <summary>Reads provider observations for an authorized account and its final publication receipt.</summary>
public interface ISocialMetricsBroker
{
    string Provider { get; }
    Task<SocialMetricsResult> ReadAsync(SocialMetricsRequest input, CancellationToken cancellationToken);
}
