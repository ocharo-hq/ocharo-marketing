using OcharoMarketing.Application.Deliveries.Models;
using OcharoMarketing.Application.Deliveries.Queries;
using OcharoMarketing.Application.Deliveries.Repositories;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Deliveries.QueryHandlers;

/// <summary>Handles <see cref="PublicationGetQuery"/>.</summary>
internal sealed class PublicationGetQueryHandler(IDeliveryRepository delivery)
    : IQueryHandler<PublicationGetQuery, AppResult<PublicationOperationLookup>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<PublicationOperationLookup>> HandleAsync(
        PublicationGetQuery request,
        CancellationToken ct)
    {
        var operation = await delivery.FindOperationAsync(request.ScheduleId, ct);

        return AppResult<PublicationOperationLookup>.Ok(new PublicationOperationLookup(operation));
    }
}
