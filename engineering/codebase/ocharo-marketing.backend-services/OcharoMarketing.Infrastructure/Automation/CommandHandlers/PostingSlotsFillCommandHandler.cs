using OcharoMarketing.Application.Automation.Commands;
using OcharoMarketing.Application.Automation.Models;
using OcharoMarketing.Infrastructure.Automation;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Automation.CommandHandlers;

/// <summary>Handles <see cref="PostingSlotsFillCommand"/>.</summary>
internal sealed class PostingSlotsFillCommandHandler(AutomationService automation)
    : ICommandHandler<PostingSlotsFillCommand, AppResult<SlotFillReport>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<SlotFillReport>> HandleAsync(PostingSlotsFillCommand request, CancellationToken ct)
    {
        var data = await automation.FillAsync(request.Days, ct);

        return AppResult<SlotFillReport>.Ok(data);
    }
}
