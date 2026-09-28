using System.Globalization;
using System.Text.RegularExpressions;
using OcharoMarketing.Application.Cards.Services;
using OcharoMarketing.Application.Marketing;
using OcharoMarketing.Application.Marketing.Models;
using OcharoMarketing.Application.Marketing.Repositories;
using OcharoMarketing.Infrastructure.Marketing.Services;

namespace OcharoMarketing.Infrastructure.Automation;

/// <summary>
/// Posting rhythm and post creation at scale. Slots turn approved drafts into <em>plans</em> through the normal
/// scheduling path; external delivery still requires a verified destination and an explicit queue confirmation.
/// </summary>
public sealed partial class AutomationService(IMarketingRepository repository, MarketingService marketing, ICardRenderingService cards, TimeProvider time)
{
    private const string SettingsKey = "automation";
    private static readonly string[] SlotChannels = ["instagram", "telegram", "x"];
    private static readonly string[] DraftChannels = ["instagram", "telegram", "whatsapp", "x"];
    private static readonly TimeZoneInfo Tashkent = FindTashkent();

    public static readonly IReadOnlyDictionary<string, string> DefaultTemplates = new Dictionary<string, string>
    {
        ["uz"] = "{name}\n\nMato: {fabric}\nBezak: {decoration}\nO‘lchamlar: {sizes}\nMavjud: {available}\nNarx: {price}\n\nBuyurtma va savollar uchun yozing.",
        ["ru"] = "{name}\n\nТкань: {fabric}\nОтделка: {decoration}\nРазмеры: {sizes}\nВ наличии: {available}\nЦена: {price}\n\nПишите нам для заказа и вопросов.",
        ["en"] = "{name}\n\nFabric: {fabric}\nDetails: {decoration}\nSizes: {sizes}\nAvailable: {available}\nPrice: {price}\n\nMessage us to order.",
    };

    public async Task<AutomationSettingsModel> GetAsync(CancellationToken ct) =>
        await repository.GetSettingAsync(SettingsKey, ct) is { } json ? MarketingJson.Read<AutomationSettingsModel>(json) : Default();

    public async Task<AutomationSettingsModel> SaveAsync(AutomationSettingsModel settings, CancellationToken ct)
    {
        if (settings.Slots.Length > 40) throw new MarketingException("Keep at most 40 posting slots.");
        foreach (var slot in settings.Slots)
        {
            if (!SlotChannels.Contains(slot.Channel)) throw new MarketingException("Slots support Instagram, Telegram and X; WhatsApp is customer messaging.");
            if (slot.Days.Length == 0 || slot.Days.Any(d => d is < 1 or > 7)) throw new MarketingException("Choose weekdays 1 (Monday) to 7 (Sunday).");
            if (!TimeOnly.TryParseExact(slot.Time, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) throw new MarketingException("Use HH:mm time for slots.");
        }

        if (!cards.GetPresets().Any(p => p.Id == settings.DefaultPresetId)) throw new MarketingException("Choose an existing card preset.");
        if (settings.DefaultChannels.Length == 0 || settings.DefaultChannels.Except(DraftChannels).Any()) throw new MarketingException("Choose supported default channels.");
        if (settings.DefaultLocale is not ("uz" or "ru" or "en")) throw new MarketingException("Choose uz, ru or en.");
        if (settings.CaptionTemplates.Any(t => t.Value.Length > 4000 || t.Key is not ("uz" or "ru" or "en"))) throw new MarketingException("Templates are uz/ru/en and under 4000 characters.");
        if (settings.AutoSyncHours is < 0 or > 168) throw new MarketingException("Automatic catalogue sync runs every 1–168 hours (0 turns it off).");
        if (settings.AutoFillDays is < 0 or > 60) throw new MarketingException("Automatic planning looks 1–60 days ahead (0 turns it off).");
        var normalized = settings with
        {
            Slots = settings.Slots.Select(s => s with { Id = string.IsNullOrWhiteSpace(s.Id) ? Guid.NewGuid().ToString("N")[..8] : s.Id, Days = s.Days.Distinct().Order().ToArray() }).ToArray(),
        };
        await repository.SaveSettingAsync(SettingsKey, MarketingJson.Write(normalized), ct);
        return normalized;
    }

    /// <summary>Plans approved drafts into the next free slots of each of their channels.</summary>
    public async Task<SlotFillReport> FillAsync(int days, CancellationToken ct)
    {
        if (days is < 1 or > 60) throw new MarketingException("Plan between 1 and 60 days ahead.");
        var settings = await GetAsync(ct);
        var now = time.GetUtcNow();
        var schedules = (await repository.GetSchedulesAsync(ct)).Where(s => s.Status != "cancelled").ToList();
        var taken = schedules.Select(s => (s.Channel, s.ScheduledAt.ToUnixTimeSeconds() / 60)).ToHashSet();
        var covered = schedules.Select(s => (s.DraftId, s.Channel)).ToHashSet();
        var free = settings.Slots.Where(s => s.Enabled).SelectMany(slot => Occurrences(slot, now, days).Select(at => (slot.Channel, At: at)))
            .Where(x => !taken.Contains((x.Channel, x.At.ToUnixTimeSeconds() / 60)))
            .GroupBy(x => x.Channel)
            .ToDictionary(g => g.Key, g => new Queue<DateTimeOffset>(g.Select(x => x.At).Distinct().Order()));
        var planned = new List<ScheduleModel>();
        var skipped = new List<string>();
        var drafts = (await repository.GetDraftsAsync(ct)).Where(d => d.ApprovedRevision == d.Revision).OrderBy(d => d.UpdatedAt);
        foreach (var draft in drafts)
        foreach (var channel in draft.Channels.Where(SlotChannels.Contains))
        {
            if (covered.Contains((draft.Id, channel))) continue;
            if (!free.TryGetValue(channel, out var queue) || queue.Count == 0)
            {
                skipped.Add($"{draft.Title}: {channel} — bo‘sh vaqt yo‘q");
                continue;
            }

            var schedule = await marketing.ScheduleAsync(draft.Id, new ScheduleInput(draft.Revision, channel, queue.Dequeue()), ct);
            planned.Add(schedule with { DraftTitle = draft.Title });
            covered.Add((draft.Id, channel));
        }

        return new SlotFillReport([.. planned], [.. skipped]);
    }

    /// <summary>Creates one draft per product with the locale's caption template.</summary>
    public async Task<DraftModel[]> CreateDraftsAsync(BulkDraftInput input, CancellationToken ct)
    {
        if (input.ProductIds.Length is < 1 or > 60) throw new MarketingException("Choose between 1 and 60 products.");
        var settings = await GetAsync(ct);
        var brand = await repository.GetBrandAsync(ct);
        var template = string.IsNullOrWhiteSpace(input.Template)
            ? settings.CaptionTemplates.GetValueOrDefault(input.Locale) ?? DefaultTemplates[input.Locale is "ru" or "en" ? input.Locale : "uz"]
            : input.Template;
        var drafts = new List<DraftModel>();
        foreach (var id in input.ProductIds.Distinct())
        {
            var product = await repository.FindProductAsync(id, ct) ?? throw new MarketingException($"Product '{id}' not found.", 404);
            var name = product.Copy?.GetValueOrDefault(input.Locale)?.Name ?? product.Name;
            drafts.Add(await marketing.SaveDraftAsync(null, new DraftInput($"{name} · {product.Id}", product.Id, input.PresetId, input.Locale,
                Render(template, product, input.Locale, brand), input.Channels, [product.HeroAssetId]), ct));
        }

        return [.. drafts];
    }

    /// <summary>Renders the saved template for one product and language (the composer's "from template" caption).</summary>
    public async Task<string> CaptionAsync(string productId, string locale, CancellationToken ct)
    {
        if (locale is not ("uz" or "ru" or "en")) throw new MarketingException("Choose uz, ru or en.");
        var product = await repository.FindProductAsync(productId, ct) ?? throw new MarketingException("Product not found.", 404);
        var settings = await GetAsync(ct);
        var template = settings.CaptionTemplates.GetValueOrDefault(locale) ?? DefaultTemplates[locale];
        return Render(template, product, locale, await repository.GetBrandAsync(ct));
    }

    /// <summary>Fills {name} {fabric} {decoration} {description} {sizes} {available} {price} {wholesale} {brand} {id}; drops lines whose value is empty.</summary>
    public static string Render(string template, ProductModel product, string locale, BrandModel brand)
    {
        var copy = product.Copy?.GetValueOrDefault(locale);
        var values = new Dictionary<string, string?>
        {
            ["name"] = copy?.Name ?? product.Name,
            ["fabric"] = copy?.Fabric ?? (locale == "uz" ? product.Material : null),
            ["decoration"] = copy?.Decoration,
            ["description"] = copy?.Description ?? (locale == "uz" ? product.Description : null),
            ["sizes"] = product.Sizes.Length == 0 ? null : string.Join(" · ", product.Sizes),
            ["price"] = Price(product.Price, locale),
            ["wholesale"] = Price(product.WholesalePrice, locale),
            ["brand"] = brand.Name,
            ["id"] = product.Id,
            ["available"] = product.Availability switch
            {
                "in-stock" when product.AvailableSizes is { Length: > 0 } sizes => string.Join(" · ", sizes),
                "made-to-order" => locale switch { "ru" => "под заказ", "en" => "made to order", _ => "buyurtma asosida" },
                _ => null,
            },
        };
        var lines = template.Replace("\r\n", "\n").Split('\n').Select(line =>
        {
            var keys = Placeholder().Matches(line).Select(m => m.Groups[1].Value).ToList();
            if (keys.Any(k => values.ContainsKey(k) && string.IsNullOrWhiteSpace(values[k]))) return null;
            return Placeholder().Replace(line, m => values.TryGetValue(m.Groups[1].Value, out var v) ? v ?? "" : m.Value);
        }).OfType<string>();
        return Regex.Replace(string.Join('\n', lines), "\n{3,}", "\n\n").Trim();
    }

    public static string? Price(decimal? value, string locale)
    {
        if (value is not { } amount) return null;
        var grouped = amount.ToString("N0", CultureInfo.InvariantCulture);
        return locale switch
        {
            "ru" => grouped.Replace(',', ' ') + " сум",
            "en" => grouped + " UZS",
            _ => grouped.Replace(',', ' ') + " so‘m",
        };
    }

    /// <summary>Slot times (UTC) from now through the horizon, at least five minutes ahead.</summary>
    public static IEnumerable<DateTimeOffset> Occurrences(PostingSlotModel slot, DateTimeOffset now, int days)
    {
        var time = TimeOnly.ParseExact(slot.Time, "HH:mm", CultureInfo.InvariantCulture);
        var localToday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, Tashkent).DateTime);
        for (var offset = 0; offset <= days; offset++)
        {
            var date = localToday.AddDays(offset);
            var iso = date.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)date.DayOfWeek;
            if (!slot.Days.Contains(iso)) continue;
            var local = date.ToDateTime(time);
            var at = new DateTimeOffset(local, Tashkent.GetUtcOffset(local)).ToUniversalTime();
            if (at > now.AddMinutes(5)) yield return at;
        }
    }

    public static AutomationSettingsModel Default() => new(
        [
            new PostingSlotModel("tg-evening", "telegram", [1, 3, 5], "19:00", true),
            new PostingSlotModel("ig-evening", "instagram", [2, 4, 6], "20:00", true),
        ],
        DraftNewCatalogueProducts: false,
        DefaultPresetId: "t02-bone",
        DefaultChannels: ["instagram", "telegram"],
        DefaultLocale: "uz",
        CaptionTemplates: DefaultTemplates);

    private static TimeZoneInfo FindTashkent()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Tashkent"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.CreateCustomTimeZone("Asia/Tashkent", TimeSpan.FromHours(5), "Tashkent", "Tashkent"); }
    }

    [GeneratedRegex(@"\{([a-z]+)\}")]
    private static partial Regex Placeholder();
}
