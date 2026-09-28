namespace OcharoMarketing.Application.Providers;

/// <summary>Reads existing receipt evidence without creating or retrying publication.</summary>
public interface IReceiptReconciliationBroker
{
    string Provider { get; }
    Task<ReceiptReconciliationResult> ReconcileAsync(ReceiptReconciliationRequest input, CancellationToken cancellationToken);
}
