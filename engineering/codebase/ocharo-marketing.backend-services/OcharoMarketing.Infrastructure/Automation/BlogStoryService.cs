using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Application.Marketing.Repositories;
using OcharoMarketing.Infrastructure.Marketing;
using OcharoMarketing.Infrastructure.Marketing.Services;

namespace OcharoMarketing.Infrastructure.Automation;

/// <summary>Reads published Ocharo Blog stories (RSS) and turns one into a draft post that links back to it.</summary>
public sealed class BlogStoryService(IMarketingRepository repository, MarketingService marketing, HttpClient http, IOptions<MarketingOptions> options)
{
    private const string AnnouncedKey = "blog-announced";

    public async Task<BlogStoryModel[]> ListAsync(CancellationToken ct)
    {
        var origin = options.Value.BlogBaseUrl?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(origin)) throw new MarketingException("Blog manzili sozlanmagan (Marketing:BlogBaseUrl).", 409);
        XDocument feed;
        try
        {
            await using var stream = await http.GetStreamAsync($"{origin}/feed.xml", ct);
            feed = await XDocument.LoadAsync(stream, LoadOptions.None, ct);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or XmlException)
        {
            throw new MarketingException($"Ocharo Blogga ulanib bo‘lmadi ({origin}). Blog xizmati ishlayotganini tekshiring.", 502);
        }

        var announced = await AnnouncedAsync(ct);
        return feed.Descendants("item").Select(item =>
        {
            var link = item.Element("link")?.Value ?? "";
            if (link.StartsWith('/')) link = origin + link;
            DateTimeOffset? published = DateTimeOffset.TryParse(item.Element("pubDate")?.Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var at) ? at : null;
            return new BlogStoryModel(item.Element("title")?.Value ?? "", link, item.Element("description")?.Value ?? "", item.Element("category")?.Value,
                published, announced.GetValueOrDefault(link));
        }).Where(s => s.Link.Length > 0).ToArray();
    }

    public async Task<DraftModel> DraftAsync(StoryDraftInput input, CancellationToken ct)
    {
        var story = (await ListAsync(ct)).FirstOrDefault(s => s.Link == input.Link) ?? throw new MarketingException("Story not found in the blog feed.", 404);
        var product = await repository.FindProductAsync(input.ProductId, ct) ?? throw new MarketingException("Product not found.", 404);
        var read = input.Locale switch { "ru" => "Читать", "en" => "Read", _ => "O‘qish" };
        var caption = $"{story.Title}\n\n{story.Excerpt}\n\n{read}: {story.Link}";
        var draft = await marketing.SaveDraftAsync(null, new DraftInput($"Blog · {story.Title}", product.Id, input.PresetId, input.Locale, caption,
            input.Channels, [product.HeroAssetId]), ct);
        var announced = await AnnouncedAsync(ct);
        announced[story.Link] = draft.Id;
        await repository.SaveSettingAsync(AnnouncedKey, MarketingJson.Write(announced), ct);
        return draft;
    }

    private async Task<Dictionary<string, string>> AnnouncedAsync(CancellationToken ct) =>
        await repository.GetSettingAsync(AnnouncedKey, ct) is { } json ? MarketingJson.Read<Dictionary<string, string>>(json) : [];
}
