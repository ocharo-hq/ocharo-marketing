namespace OcharoMarketing.Application.Marketing.Models;

/// <summary>Defines safe durable delivery workflow data.</summary>
public sealed record DeliveryIdentityModel(string ConnectionId, string Provider, int Revision, string AccountId, string DestinationId, string DisplayName, string[] Capabilities, DateTimeOffset VerifiedAt);
