namespace OcharoBlog.Application.Blog.Models;

/// <summary>Catalogue content locales. Availability is determined by the active Platform release.</summary>
public static class CatalogueLanguages
{
    public static IReadOnlyDictionary<string, string> Names { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["uz"] = "O‘zbekcha", ["ru"] = "Русский", ["en"] = "English", ["tg"] = "Тоҷикӣ",
        ["kk"] = "Қазақша", ["ky"] = "Кыргызча", ["tk"] = "Türkmençe", ["tr"] = "Türkçe",
        ["az"] = "Azərbaycanca", ["fr"] = "Français", ["it"] = "Italiano", ["de"] = "Deutsch",
        ["es"] = "Español", ["pt"] = "Português", ["pl"] = "Polski", ["uk"] = "Українська",
        ["ar"] = "العربية", ["fa"] = "فارسی", ["ur"] = "اردو", ["zh-Hans"] = "简体中文",
        ["ko"] = "한국어", ["ja"] = "日本語", ["id"] = "Bahasa Indonesia", ["ms"] = "Bahasa Melayu", ["hi"] = "हिन्दी"
    };
    public static bool Supported(string? locale) => locale is not null && Names.ContainsKey(locale);
    public static string Normalize(string? locale) => Supported(locale) ? locale! : "en";
    public static string Direction(string locale) => locale is "ar" or "fa" or "ur" ? "rtl" : "ltr";
}
