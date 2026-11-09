using OcharoBlog.Api.Blog.Errors;

namespace OcharoBlog.Tests.Unit;

/// <summary>Editorial answers each refusal with the status it has always answered it with.</summary>
public sealed class BlogErrorHttpStatusCodeMapperTests
{
    private readonly BlogErrorHttpStatusCodeMapper mapper = new();

    [Fact]
    public void ToStatusCode_ShouldReturn413_WhenTheImageIsTooLarge()
    {
        mapper.ToStatusCode(BlogErrors.TooLarge("Image exceeds 20 MiB.")).Should().Be(413);
    }

    [Fact]
    public void ToStatusCode_ShouldReturn502_WhenFilesRefusedTheCall()
    {
        mapper.ToStatusCode(BlogErrors.FilesRefused("WoW2 Files refused the call: quota.")).Should().Be(502);
    }

    [Fact]
    public void ToStatusCode_ShouldReturn503_WhenADependencyIsUnavailable()
    {
        mapper.ToStatusCode(BlogErrors.Unavailable("Background removal is unavailable.")).Should().Be(503);
    }

    [Fact]
    public void ToStatusCode_ShouldKeepTheUnexpectedCode_WhenADependencyIsUnavailable()
    {
        BlogErrors.Unavailable("Background removal is unavailable.").Type.Should().Be(AppErrorType.Unexpected);
        BlogErrors.FilesRefused("WoW2 Files refused the call.").Type.Should().Be(AppErrorType.Unexpected);
    }

    [Fact]
    public void ToStatusCode_ShouldUseTheSdkDefaults_WhenTheErrorCarriesNoMark()
    {
        mapper.ToStatusCode(BlogErrors.Invalid("Language must be en, uz or ru.")).Should().Be(400);
        mapper.ToStatusCode(BlogErrors.Unauthorized("Your connection expired.")).Should().Be(401);
        mapper.ToStatusCode(BlogErrors.Forbidden("Editor access is required.")).Should().Be(403);
        mapper.ToStatusCode(BlogErrors.NotFound("Story")).Should().Be(404);
        mapper.ToStatusCode(BlogErrors.Conflict("The story changed.")).Should().Be(409);
        mapper.ToStatusCode(BlogErrors.Busy("Image processing is busy.")).Should().Be(429);
    }
}
