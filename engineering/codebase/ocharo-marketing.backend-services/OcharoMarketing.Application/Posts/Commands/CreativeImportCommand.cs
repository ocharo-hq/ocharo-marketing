using OcharoMarketing.Application.Posts.Models;
using WoW.Two.Sdk.Beta.Be.Mediator.Cqrs;
using WoW.Two.Sdk.Beta.Be.Mediator.Result;

namespace OcharoMarketing.Application.Posts.Commands;

/// <summary>Represents a command that imports an Ocharo Studio review export as a draft post.</summary>
public sealed record CreativeImportCommand : ICommand<AppResult<CreativeImportModel>>
{
    /// <summary>Gets the Studio review manifest's bytes.</summary>
    public required byte[] Manifest { get; init; }

    /// <summary>Gets the exported PNG's bytes.</summary>
    public required byte[] Image { get; init; }

    /// <summary>Gets the existing product the draft belongs to.</summary>
    public required string ProductId { get; init; }

    /// <summary>Gets the organization that received the export.</summary>
    public string? OrganizationId { get; init; }

    /// <summary>Gets the signed-in person who received the export.</summary>
    public string? UserId { get; init; }
}
