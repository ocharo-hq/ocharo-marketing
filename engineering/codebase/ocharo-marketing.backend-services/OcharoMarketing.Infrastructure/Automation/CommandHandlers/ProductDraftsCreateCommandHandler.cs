using OcharoMarketing.Application.Automation.Commands;
using OcharoMarketing.Application.Posts.Models;
using OcharoMarketing.Infrastructure.Automation;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Automation.CommandHandlers;

/// <summary>Handles <see cref="ProductDraftsCreateCommand"/>.</summary>
internal sealed class ProductDraftsCreateCommandHandler(AutomationService automation)
    : ICommandHandler<ProductDraftsCreateCommand, AppResult<DraftModel[]>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<DraftModel[]>> HandleAsync(
        ProductDraftsCreateCommand request,
        CancellationToken ct)
    {
        var data = await automation.CreateDraftsAsync(request.Input, ct);

        return AppResult<DraftModel[]>.Ok(data);
    }
}
