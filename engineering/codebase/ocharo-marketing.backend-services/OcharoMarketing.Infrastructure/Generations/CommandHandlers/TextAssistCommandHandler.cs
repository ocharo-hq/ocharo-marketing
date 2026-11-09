using OcharoMarketing.Application.Generations.Commands;
using OcharoMarketing.Application.Generations.Services;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Infrastructure.Generations.CommandHandlers;

/// <summary>Handles <see cref="TextAssistCommand"/>.</summary>
internal sealed class TextAssistCommandHandler(IExternalMarketingService external)
    : ICommandHandler<TextAssistCommand, AppResult<object>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<object>> HandleAsync(TextAssistCommand request, CancellationToken ct)
    {
        var data = await external.AssistAsync(request.Prompt, request.Operation, request.Locale, ct);

        return AppResult<object>.Ok(data);
    }
}
