using OcharoBlog.Application.Blog;
using OcharoBlog.Application.Blog.Models;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Domain.Blog;

namespace OcharoBlog.Tests.Integration;

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
    public async Task Seed_ShouldPublishThirtyStoriesInJournalOrder_WhenTheBlogIsEmpty()
    {
        await using var harness = new BlogHarness();
        _ = harness.Client();
        var list =
            await harness.Get<ArticleService>().ListAsync(ArticleStatus.Published, null, null, CancellationToken.None);

        list.Count.Should().Be(30);
        list[0].Slug.Should().Be("the-nimcha-in-a-new-light");
        list.Should().Contain(a => a.HasCover);
    }

    [Fact]
    public async Task Update_ShouldRefuseAsConflict_WhenTheRevisionIsStale()
    {
        await using var harness = new BlogHarness();
        _ = harness.Client();
        var articles = harness.Get<ArticleService>();

        var created = (await articles.CreateAsync(Story(), CancellationToken.None)).ValueOrThrow();
        var updated =
            (await articles.UpdateAsync(
                created.Id,
                Story() with { ExpectedRevision = 1, Title = "Renamed" },
                CancellationToken.None))
                .ValueOrThrow();
        var stale =
            Refusals.Of(await articles.UpdateAsync(
                created.Id,
                Story() with { ExpectedRevision = 1 },
                CancellationToken.None));
        var duplicate =
            Refusals.Of(await articles.CreateAsync(Story("the-nimcha-in-a-new-light"), CancellationToken.None));

        created.Document.Status.Should().Be(ArticleStatus.Draft);
        updated.Revision.Should().Be(2);
        stale.Type.Should().Be(AppErrorType.Conflict);
        duplicate.Type.Should().Be(AppErrorType.Conflict);
    }

    [Fact]
    public async Task Create_ShouldRefuse_WhenSourcesCitationsOrRelatedLinksAreBad()
    {
        await using var harness = new BlogHarness();
        _ = harness.Client();
        var articles = harness.Get<ArticleService>();

        var http =
            Refusals.Of(await articles.CreateAsync(
                Story() with { Sources = [new("x", "http://insecure.example", null)] },
                CancellationToken.None));
        var citation = Refusals.Of(await articles.CreateAsync(Story() with { Sources = [] }, CancellationToken.None));
        var related =
            Refusals.Of(await articles.CreateAsync(Story() with { RelatedSlugs = ["nope"] }, CancellationToken.None));

        http.Message.Should().Contain("https");
        citation.Message.Should().Contain("missing source");
        related.Message.Should().Contain("does not exist");
    }

    [Fact]
    public async Task Publish_ShouldRequireContentAndPublishOnSchedule_WhenDue()
    {
        await using var harness = new BlogHarness();
        _ = harness.Client();
        var articles = harness.Get<ArticleService>();
        var empty =
            (await articles.CreateAsync(
                Story("empty-story") with { Intro = null, Sections = [], Sources = [] },
                CancellationToken.None))
                .ValueOrThrow();
        var story = (await articles.CreateAsync(Story(), CancellationToken.None)).ValueOrThrow();

        var refused = Refusals.Of(await articles.PublishAsync(empty.Id, 1, CancellationToken.None));
        var publishAt = harness.Clock.GetUtcNow().AddHours(2);
        var scheduled = (await articles.ScheduleAsync(story.Id, 1, publishAt, CancellationToken.None)).ValueOrThrow();
        var early = await articles.PublishDueAsync(CancellationToken.None);
        harness.Clock.Advance(TimeSpan.FromHours(3));
        var due = await articles.PublishDueAsync(CancellationToken.None);
        var live = (await articles.GetAsync(story.Id, CancellationToken.None)).ValueOrThrow();

        refused.Message.Should().Contain("introduction");
        scheduled.Document.Status.Should().Be(ArticleStatus.Scheduled);
        early.Should().BeEmpty();
        due.Should().Equal(["a-new-story"]);
        live.Document.Status.Should().Be(ArticleStatus.Published);
        live.Document.PublishedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Restore_ShouldBringBackContentButKeepLiveState_WhenTheStoryIsLive()
    {
        await using var harness = new BlogHarness();
        _ = harness.Client();
        var articles = harness.Get<ArticleService>();
        var story = (await articles.CreateAsync(Story(), CancellationToken.None)).ValueOrThrow();
        var published = (await articles.PublishAsync(story.Id, 1, CancellationToken.None)).ValueOrThrow();
        (await articles.UpdateAsync(
            story.Id,
            Story() with { ExpectedRevision = published.Revision, Title = "Changed" },
            CancellationToken.None))
            .ValueOrThrow();

        var restored =
            (await articles.RestoreAsync(story.Id, 1, published.Revision + 1, CancellationToken.None)).ValueOrThrow();

        restored.Document.Title.Should().Be("A new story");
        restored.Document.Status.Should().Be(ArticleStatus.Published);
    }
}
