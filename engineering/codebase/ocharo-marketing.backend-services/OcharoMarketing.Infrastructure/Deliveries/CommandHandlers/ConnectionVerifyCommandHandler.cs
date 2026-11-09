using OcharoMarketing.Application.Deliveries.Commands;
using OcharoMarketing.Application.Deliveries.Models;
using OcharoMarketing.Infrastructure.Deliveries.Services;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Deliveries.CommandHandlers;

/// <summary>Handles <see cref="ConnectionVerifyCommand"/>.</summary>
internal sealed class ConnectionVerifyCommandHandler(PublicationService publication)
    : ICommandHandler<ConnectionVerifyCommand, AppResult<ConnectionModel>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<ConnectionModel>> HandleAsync(
        ConnectionVerifyCommand request,
        CancellationToken ct)
    {
        var data = await publication.VerifyAsync(request.Id, ct);

        return AppResult<ConnectionModel>.Ok(data);
    }
}
