using OcharoBlog.Application.Blog.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoBlog.Application.Blog.Queries;

/// <summary>Represents a query to get the public site's settings with their revision stamp.</summary>
public sealed record SiteSettingsGetQuery : IQuery<AppResult<SettingsEnvelope>>;
