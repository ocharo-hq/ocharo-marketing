using OcharoBlog.Application.Blog;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Domain.Blog;

namespace OcharoBlog.Tests;

public sealed class ArticleLifecycleTests
{
    private static ArticleInput Story(string slug = "a-new-story") => new()
    {
        Slug = slug,
        Topic = "style",
        Title = "A new story",
        Excerpt = "An excerpt.",
        Intro = "An introduction.",
        Sections = [new ArticleSection("First", ["One paragraph."], null, [0])],
        Sources = [new ArticleSource("A museum record", "https://example.org/record", null)],
    };

    [Fact]
    public async Task Seeds_thirty_published_stories_in_journal_order()
    {
        await using var harness = new BlogHarness();
        _ = harness.Client();
        var list = await harness.Get<ArticleService>().ListAsync(ArticleStatus.Published, null, null, CancellationToken.None);

        Assert.Equal(30, list.Count);
        Assert.Equal("the-nimcha-in-a-new-light", list[0].Slug);
        Assert.Contains(list, a => a.HasCover);
    }

    [Fact]
    public async Task Create_update_and_stale_revision_conflict()
    {
        await using var harness = new BlogHarness();
        _ = harness.Client();
        var articles = harness.Get<ArticleService>();

        var created = await articles.CreateAsync(Story(), CancellationToken.None);
        var updated = await articles.UpdateAsync(created.Id, Story() with { ExpectedRevision = 1, Title = "Renamed" }, CancellationToken.None);
        var stale = await Assert.ThrowsAsync<BlogException>(() => articles.UpdateAsync(created.Id, Story() with { ExpectedRevision = 1 }, CancellationToken.None));
        var duplicate = await Assert.ThrowsAsync<BlogException>(() => articles.CreateAsync(Story("the-nimcha-in-a-new-light"), CancellationToken.None));

        Assert.Equal(ArticleStatus.Draft, created.Document.Status);
        Assert.Equal(2, updated.Revision);
        Assert.Equal(409, stale.StatusCode);
        Assert.Equal(409, duplicate.StatusCode);
    }

    [Fact]
    public async Task Rejects_bad_sources_citations_and_related_links()
    {
        await using var harness = new BlogHarness();
        _ = harness.Client();
        var articles = harness.Get<ArticleService>();

        var http = await Assert.ThrowsAsync<BlogException>(() => articles.CreateAsync(Story() with { Sources = [new("x", "http://insecure.example", null)] }, CancellationToken.None));
        var citation = await Assert.ThrowsAsync<BlogException>(() => articles.CreateAsync(Story() with { Sources = [] }, CancellationToken.None));
        var related = await Assert.ThrowsAsync<BlogException>(() => articles.CreateAsync(Story() with { RelatedSlugs = ["nope"] }, CancellationToken.None));

        Assert.Contains("https", http.Message);
        Assert.Contains("missing source", citation.Message);
        Assert.Contains("does not exist", related.Message);
    }

    [Fact]
    public async Task Publishing_requires_content_and_scheduling_publishes_when_due()
    {
        await using var harness = new BlogHarness();
        _ = harness.Client();
        var articles = harness.Get<ArticleService>();
        var empty = await articles.CreateAsync(Story("empty-story") with { Intro = null, Sections = [], Sources = [] }, CancellationToken.None);
        var story = await articles.CreateAsync(Story(), CancellationToken.None);

        var refused = await Assert.ThrowsAsync<BlogException>(() => articles.PublishAsync(empty.Id, 1, CancellationToken.None));
        var scheduled = await articles.ScheduleAsync(story.Id, 1, harness.Clock.UtcNow.AddHours(2), CancellationToken.None);
        var early = await articles.PublishDueAsync(CancellationToken.None);
        harness.Clock.UtcNow = harness.Clock.UtcNow.AddHours(3);
        var due = await articles.PublishDueAsync(CancellationToken.None);
        var live = await articles.GetAsync(story.Id, CancellationToken.None);

        Assert.Contains("introduction", refused.Message);
        Assert.Equal(ArticleStatus.Scheduled, scheduled.Document.Status);
        Assert.Empty(early);
        Assert.Equal(["a-new-story"], due);
        Assert.Equal(ArticleStatus.Published, live.Document.Status);
        Assert.NotNull(live.Document.PublishedAt);
    }

    [Fact]
    public async Task Restore_brings_back_content_without_changing_live_state()
    {
        await using var harness = new BlogHarness();
        _ = harness.Client();
        var articles = harness.Get<ArticleService>();
        var story = await articles.CreateAsync(Story(), CancellationToken.None);
        var published = await articles.PublishAsync(story.Id, 1, CancellationToken.None);
        await articles.UpdateAsync(story.Id, Story() with { ExpectedRevision = published.Revision, Title = "Changed" }, CancellationToken.None);

        var restored = await articles.RestoreAsync(story.Id, 1, published.Revision + 1, CancellationToken.None);

        Assert.Equal("A new story", restored.Document.Title);
        Assert.Equal(ArticleStatus.Published, restored.Document.Status);
    }
}
