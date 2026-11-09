using OcharoBlog.Application.Blog.Commands;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Infrastructure.Blog.Extensions;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Infrastructure.Blog.CommandHandlers;

/// <summary>Handles <see cref="SiteSettingsSaveCommand"/>.</summary>
internal sealed class SiteSettingsSaveCommandHandler(SiteSettingsService settings)
    : ICommandHandler<SiteSettingsSaveCommand, AppResult<SettingsEnvelope>>
{
    /// <inheritdoc />
    public async ValueTask<AppResult<SettingsEnvelope>> HandleAsync(
        SiteSettingsSaveCommand request,
        CancellationToken ct)
    {
        return (await settings.SaveAsync(request.Settings, request.ExpectedRevision, ct)).ToAppResult();
    }
}
