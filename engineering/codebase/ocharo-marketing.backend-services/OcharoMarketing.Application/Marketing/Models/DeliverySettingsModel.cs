namespace OcharoMarketing.Application.Marketing.Models;

/// <summary>Defines safe durable delivery workflow data.</summary>
public sealed record DeliverySettingsModel(bool PublishingEnabled, bool MetricReadsEnabled, bool AccountVerificationEnabled);
