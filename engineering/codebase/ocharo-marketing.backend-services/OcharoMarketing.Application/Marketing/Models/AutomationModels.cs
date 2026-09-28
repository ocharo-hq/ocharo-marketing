namespace OcharoMarketing.Application.Marketing.Models;

/// <summary>A recurring publishing slot for one channel, in the brand's timezone (Asia/Tashkent).</summary>
public sealed record PostingSlotModel(string Id, string Channel, int[] Days, string Time, bool Enabled);

/// <summary>Posting rhythm and draft-creation rules; slots only create plans — delivery still needs explicit queueing.</summary>
public sealed record AutomationSettingsModel(
    PostingSlotModel[] Slots,
    bool DraftNewCatalogueProducts,
    string DefaultPresetId,
    string[] DefaultChannels,
    string DefaultLocale,
    IReadOnlyDictionary<string, string> CaptionTemplates,
    int AutoSyncHours = 0,
    int AutoFillDays = 0);

/// <summary>Last unattended automation passes (kept apart from settings so saving settings never erases it).</summary>
public sealed record AutomationRunModel(
    DateTimeOffset? LastSyncAt,
    CatalogueSyncReport? LastSync,
    DateTimeOffset? LastFillAt,
    int LastFillPlanned,
    string? LastError,
    DateTimeOffset? LastErrorAt);

/// <summary>What a catalogue sync did.</summary>
public sealed record CatalogueSyncReport(int Received, int Created, int Updated, int ImagesStored, string[] NewProductIds, string[] DraftIds, string[] Warnings);

/// <summary>Creates one draft post per product from a caption template.</summary>
public sealed record BulkDraftInput(string[] ProductIds, string Locale, string PresetId, string[] Channels, string? Template);

/// <summary>Result of filling posting slots with approved drafts.</summary>
public sealed record SlotFillReport(ScheduleModel[] Planned, string[] Skipped);

/// <summary>A published Ocharo Blog story from its RSS feed.</summary>
public sealed record BlogStoryModel(string Title, string Link, string Excerpt, string? Topic, DateTimeOffset? PublishedAt, string? DraftId);

/// <summary>Creates a draft post announcing a blog story, using a product image as the visual.</summary>
public sealed record StoryDraftInput(string Link, string ProductId, string[] Channels, string Locale, string PresetId);
