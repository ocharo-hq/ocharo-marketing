using OcharoBlog.Application.Blog.Services;

namespace OcharoBlog.Application.Blog.Models;

/// <summary>Where a stored image's bytes come from; <paramref name="Content"/> is none for an image Editorial does not
/// hold.</summary>
/// <param name="Content">The Files link or local bytes; none when the image is unknown or gone.</param>
public sealed record MediaOpening(MediaContent? Content);
