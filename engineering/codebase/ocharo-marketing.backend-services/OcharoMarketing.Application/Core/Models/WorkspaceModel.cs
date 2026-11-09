using OcharoMarketing.Application.Assets.Models;
using OcharoMarketing.Application.Cards.Models;
using OcharoMarketing.Application.Deliveries.Models;
using OcharoMarketing.Application.Generations.Models;
using OcharoMarketing.Application.Posts.Models;
using OcharoMarketing.Application.Products.Models;

namespace OcharoMarketing.Application.Core.Models;

/// <summary>Represents the whole marketing workspace the desk loads: records, presets, delivery state and cached
/// metrics.</summary>
/// <param name="Brand">The brand.</param>
/// <param name="Products">The products.</param>
/// <param name="Assets">The stored assets.</param>
/// <param name="Presets">The card presets.</param>
/// <param name="Drafts">The drafts.</param>
/// <param name="Schedules">The plans.</param>
/// <param name="Connections">The delivery connections.</param>
/// <param name="GenerationJobs">The generation jobs.</param>
/// <param name="PublicationOperations">The publication operations.</param>
/// <param name="DeliverySettings">The delivery settings.</param>
/// <param name="Metrics">The cached metrics, with an explained gap for each channel none is cached for.</param>
public sealed record WorkspaceModel(
    BrandModel Brand,
    IReadOnlyList<ProductModel> Products,
    IReadOnlyList<AssetModel> Assets,
    IReadOnlyList<CardPreset> Presets,
    IReadOnlyList<DraftModel> Drafts,
    IReadOnlyList<ScheduleModel> Schedules,
    IReadOnlyList<ConnectionModel> Connections,
    IReadOnlyList<GenerationJobModel> GenerationJobs,
    IReadOnlyList<PublicationOperationModel> PublicationOperations,
    DeliverySettingsModel DeliverySettings,
    IReadOnlyList<MetricModel> Metrics);
