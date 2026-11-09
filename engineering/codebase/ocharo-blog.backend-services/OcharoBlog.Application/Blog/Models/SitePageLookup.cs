using OcharoBlog.Application.Blog.Site;

namespace OcharoBlog.Application.Blog.Models;

/// <summary>A public path's page; <paramref name="Page"/> is none for a path no page answers.</summary>
/// <param name="Page">The rendered page; none for a path the public site does not serve.</param>
public sealed record SitePageLookup(SitePage? Page);
