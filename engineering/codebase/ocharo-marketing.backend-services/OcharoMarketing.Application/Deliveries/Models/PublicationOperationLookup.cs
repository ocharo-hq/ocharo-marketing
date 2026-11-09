
namespace OcharoMarketing.Application.Deliveries.Models;

/// <summary>Represents a plan's publication operation, or none while the plan was never queued.</summary>
/// <param name="Operation">The operation; none while the plan was never queued.</param>
public sealed record PublicationOperationLookup(PublicationOperationModel? Operation);
