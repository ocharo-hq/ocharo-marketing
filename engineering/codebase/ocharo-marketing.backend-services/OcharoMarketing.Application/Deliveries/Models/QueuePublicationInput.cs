namespace OcharoMarketing.Application.Deliveries.Models;

/// <summary>Defines safe durable delivery workflow data.</summary>
public sealed record QueuePublicationInput(
    int ExpectedDraftRevision,
    string ConnectionId,
    int ExpectedConnectionRevision,
    string OperationId);
