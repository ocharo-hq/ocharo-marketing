using System.Text.Json.Serialization;

namespace OcharoBlog.Application.Blog.Models;

/// <summary>A bounded non-destructive recipe in upright original-image coordinates.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record MediaEditRecipe(ImageCrop? Crop, int Rotate, bool FlipHorizontal, bool FlipVertical,
    ImageResize? Resize, ImageEncoding Output, bool RemoveBackground);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ImageCrop(int X, int Y, int Width, int Height);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ImageResize(int Width, int Height);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ImageEncoding(string Format, int Quality);
public sealed record MediaEditActor(string OrganizationId, string UserId);
public sealed record EditedImage(byte[] Bytes, string ContentType, string Extension, int Width, int Height, string? Model);
public sealed record ImageWorkerStatus(bool Available, string? Model = null);
