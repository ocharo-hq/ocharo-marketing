using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Domain.Blog;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Application.Blog.Commands;

/// <summary>Represents a command to save the public site's settings.</summary>
public sealed record SiteSettingsSaveCommand : ICommand<AppResult<SettingsEnvelope>>
{
    /// <summary>Gets the settings to store.</summary>
    public required SiteSettings Settings { get; init; }

    /// <summary>Gets the revision the editor acted on.</summary>
    public required int ExpectedRevision { get; init; }
}
