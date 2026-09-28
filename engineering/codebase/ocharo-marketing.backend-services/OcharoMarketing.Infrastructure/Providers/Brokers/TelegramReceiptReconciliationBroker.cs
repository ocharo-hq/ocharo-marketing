using OcharoMarketing.Application.Providers;

namespace OcharoMarketing.Infrastructure.Providers.Brokers;

/// <summary>Reports the absence of a general Bot API receipt lookup without sending or searching.</summary>
public sealed class TelegramReceiptReconciliationBroker : IReceiptReconciliationBroker
{
    public string Provider => "telegram";

    public Task<ReceiptReconciliationResult> ReconcileAsync(
        ReceiptReconciliationRequest input, CancellationToken cancellationToken)
        => Task.FromResult(new ReceiptReconciliationResult(BrokerOutcome.Unsupported, Provider,
            "receipt_lookup_unavailable", "Telegram has no supported general message receipt lookup in this adapter."));
}
