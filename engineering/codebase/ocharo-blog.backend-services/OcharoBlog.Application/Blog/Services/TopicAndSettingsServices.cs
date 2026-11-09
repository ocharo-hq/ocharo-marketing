using System.Text.RegularExpressions;
using OcharoBlog.Application.Blog.Errors;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Repositories;
using OcharoBlog.Domain.Blog;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;

namespace OcharoBlog.Application.Blog.Services;

/// <summary>Reading sections.</summary>
public sealed partial class TopicService(IBlogRepository repository)
{
    public async Task<IReadOnlyList<TopicDocument>> ListAsync(CancellationToken ct)
    {
        return (await repository.GetTopicsAsync(ct))
            .OrderBy(t => t.SortOrder)
            .Select(t => BlogJson.Read<TopicDocument>(t.Json))
            .ToList();
    }

    public async Task<Result<TopicDocument>> SaveAsync(TopicDocument topic, CancellationToken ct)
    {
        if (!Key().IsMatch(topic.Key) || topic.Key.Length > 40)
            return Result<TopicDocument>.Fail(BlogErrors.Invalid("Topic key must be a short lowercase slug."));
        if (string.IsNullOrWhiteSpace(topic.Name) || string.IsNullOrWhiteSpace(topic.Short))
            return Result<TopicDocument>.Fail(BlogErrors.Invalid("A topic needs a name and a short label."));
        var translations = topic.Translations?
            .Where(t => t.Key is "uz" or "ru" && !string.IsNullOrWhiteSpace(t.Value.Name))
            .ToDictionary(t => t.Key, t => Translation(t.Value));
        var normalized = topic with
        {
            Name = topic.Name.Trim(),
            Short = topic.Short.Trim(),
            Description = topic.Description?.Trim() ?? "",
            Translations = translations is { Count: > 0 } ? translations : null,
        };
        var record = new TopicRecord
        {
            Key = normalized.Key,
            SortOrder = normalized.SortOrder,
            Json = BlogJson.Write(normalized),
        };
        await repository.SaveTopicAsync(record, ct);
        return Result<TopicDocument>.Ok(normalized);
    }

    /// <summary>Deletes a topic no story uses; returns its key.</summary>
    public async Task<Result<string>> DeleteAsync(string key, CancellationToken ct)
    {
        if ((await repository.GetArticlesAsync(ct)).Any(a => a.Topic == key))
            return Result<string>.Fail(BlogErrors.Conflict("Move or archive the stories in this topic first."));
        await repository.DeleteTopicAsync(key, ct);
        return Result<string>.Ok(key);
    }

    private static TopicText Translation(TopicText text)
    {
        var name = text.Name.Trim();
        var label = string.IsNullOrWhiteSpace(text.Short) ? name : text.Short.Trim();
        return new TopicText(name, label, text.Description?.Trim() ?? "");
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
        return record is null
            ? new SettingsEnvelope(new SiteSettings(), 0)
            : new SettingsEnvelope(BlogJson.Read<SiteSettings>(record.Json), record.Revision);
    }

    public async Task<Result<SettingsEnvelope>> SaveAsync(
        SiteSettings settings,
        int expectedRevision,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(settings.Title))
            return Result<SettingsEnvelope>.Fail(BlogErrors.Invalid("The site needs a title."));
        if (settings.BaseUrl is { Length: > 0 } baseUrl
            && (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http")))
            return Result<SettingsEnvelope>.Fail(BlogErrors.Invalid("Base URL must be an absolute http(s) address."));
        foreach (var link in settings.Links)
        {
            if (!Uri.TryCreate(link.Url, UriKind.Absolute, out var linkUri) || linkUri.Scheme != Uri.UriSchemeHttps)
                return Result<SettingsEnvelope>.Fail(
                    BlogErrors.Invalid($"Footer link '{link.Label}' needs an https:// address."));
        }
        var normalized = settings with
        {
            BaseUrl = string.IsNullOrWhiteSpace(settings.BaseUrl) ? null : settings.BaseUrl.TrimEnd('/'),
        };
        var record = new SiteSettingsRecord { Id = SettingsId, Json = BlogJson.Write(normalized) };
        var saved = await repository.SaveSettingsAsync(record, expectedRevision, ct);
        return saved.Map(stored => new SettingsEnvelope(normalized, stored.Revision));
    }
}
