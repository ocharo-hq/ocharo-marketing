using System.Reflection;
using Microsoft.EntityFrameworkCore;
using OcharoBlog.Application.Blog.Repositories;
using OcharoBlog.Domain.Blog;

namespace OcharoBlog.Tests.E2E;

/// <summary>Serves every blog repository call from <see cref="Inner"/>, except that inserting the story
/// <see cref="Slug"/> fails as a save that lost a race with another save.</summary>
public class RacingBlogRepository : DispatchProxy
{
    /// <summary>The slug of the story whose insert loses the race.</summary>
    public const string Slug = "raced";

    /// <summary>The repository every other call reaches.</summary>
    public IBlogRepository Inner { get; set; } = null!;

    /// <summary>Wraps <paramref name="inner"/>.</summary>
    public static IBlogRepository Over(IBlogRepository inner)
    {
        var proxy = Create<IBlogRepository, RacingBlogRepository>();
        ((RacingBlogRepository)(object)proxy).Inner = inner;
        return proxy;
    }

    /// <inheritdoc />
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        ArgumentNullException.ThrowIfNull(targetMethod);

        return targetMethod.Name == nameof(IBlogRepository.InsertArticleAsync)
            && args?[0] is ArticleRecord { Slug: Slug }
            ? throw new DbUpdateConcurrencyException("Another save took the row.")
            : targetMethod.Invoke(Inner, args);
    }
}
