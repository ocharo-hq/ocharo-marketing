using OcharoMarketing.Application.Deliveries.Commands;
using OcharoMarketing.Application.Deliveries.Models;
using OcharoMarketing.Infrastructure.Deliveries.Services;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Deliveries.CommandHandlers;

/// <summary>Handles <see cref="ReceiptReconcileCommand"/>.</summary>
internal sealed class ReceiptReconcileCommandHandler(ReceiptReconciliationService reconciliation)
    : ICommandHandler<ReceiptReconcileCommand, AppResult<PublicationOperationModel>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<PublicationOperationModel>> HandleAsync(
        ReceiptReconcileCommand request,
        CancellationToken ct)
    {
        var data = await reconciliation.ReconcileAsync(request.ScheduleId, ct);

        return AppResult<PublicationOperationModel>.Ok(data);
    }
}
