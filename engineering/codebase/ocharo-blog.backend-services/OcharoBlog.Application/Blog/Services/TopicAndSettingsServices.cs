using System.Text.RegularExpressions;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Repositories;
using OcharoBlog.Domain.Blog;

namespace OcharoBlog.Application.Blog.Services;

/// <summary>Reading sections.</summary>
public sealed partial class TopicService(IBlogRepository repository)
{
    public async Task<IReadOnlyList<TopicDocument>> ListAsync(CancellationToken ct) =>
        (await repository.GetTopicsAsync(ct)).OrderBy(t => t.SortOrder).Select(t => BlogJson.Read<TopicDocument>(t.Json)).ToList();

    public async Task<TopicDocument> SaveAsync(TopicDocument topic, CancellationToken ct)
    {
        if (!Key().IsMatch(topic.Key) || topic.Key.Length > 40) throw new BlogException("Topic key must be a short lowercase slug.");
        if (string.IsNullOrWhiteSpace(topic.Name) || string.IsNullOrWhiteSpace(topic.Short)) throw new BlogException("A topic needs a name and a short label.");
        var translations = topic.Translations?
            .Where(t => t.Key is "uz" or "ru" && !string.IsNullOrWhiteSpace(t.Value.Name))
            .ToDictionary(t => t.Key, t => new TopicText(t.Value.Name.Trim(), string.IsNullOrWhiteSpace(t.Value.Short) ? t.Value.Name.Trim() : t.Value.Short.Trim(),
                t.Value.Description?.Trim() ?? ""));
        var normalized = topic with
        {
            Name = topic.Name.Trim(),
            Short = topic.Short.Trim(),
            Description = topic.Description?.Trim() ?? "",
            Translations = translations is { Count: > 0 } ? translations : null,
        };
        await repository.SaveTopicAsync(new TopicRecord { Key = normalized.Key, SortOrder = normalized.SortOrder, Json = BlogJson.Write(normalized) }, ct);
        return normalized;
    }

    public async Task DeleteAsync(string key, CancellationToken ct)
    {
        if ((await repository.GetArticlesAsync(ct)).Any(a => a.Topic == key)) throw BlogException.Conflict("Move or archive the stories in this topic first.");
        await repository.DeleteTopicAsync(key, ct);
    }

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    private static partial Regex Key();
}

/// <summary>Public-site presentation settings.</summary>
public sealed class SiteSettingsService(IBlogRepository repository)
{
    public const string SettingsId = "site";

    public async Task<SettingsEnvelope> GetAsync(CancellationToken ct)
    {
        var record = await repository.FindSettingsAsync(ct);
        return record is null ? new SettingsEnvelope(new SiteSettings(), 0) : new SettingsEnvelope(BlogJson.Read<SiteSettings>(record.Json), record.Revision);
    }

    public async Task<SettingsEnvelope> SaveAsync(SiteSettings settings, int expectedRevision, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(settings.Title)) throw new BlogException("The site needs a title.");
        if (settings.BaseUrl is { Length: > 0 } baseUrl && (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http")))
            throw new BlogException("Base URL must be an absolute http(s) address.");
        foreach (var link in settings.Links)
            if (!Uri.TryCreate(link.Url, UriKind.Absolute, out var linkUri) || linkUri.Scheme != Uri.UriSchemeHttps)
                throw new BlogException($"Footer link '{link.Label}' needs an https:// address.");
        var normalized = settings with { BaseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl) ? null : settings.BaseUrl.TrimEnd('/') };
        var saved = await repository.SaveSettingsAsync(new SiteSettingsRecord { Id = SettingsId, Json = BlogJson.Write(normalized) }, expectedRevision, ct);
        return new SettingsEnvelope(normalized, saved.Revision);
    }
}
