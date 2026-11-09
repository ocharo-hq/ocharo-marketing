using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using OcharoBlog.Api.Configurations;
using OcharoBlog.Application.Blog.Site;
namespace OcharoBlog.Tests.E2E;
public sealed class VersionReportingTests
{
    [Fact]
    public async Task Status_ShouldReportTheHostAssemblyVersion_WhenRead()
    {
        await using var harness = new BlogHarness();
        var result = await harness.Client().GetFromJsonAsync<JsonElement>("/api/system/status");
        var expected =
            typeof(HostConfiguration)
                .Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        result.GetProperty("version").GetString().Should().Be(expected);
    }
    [Theory]
    [InlineData("/")]
    [InlineData("/atlas/")]
    [InlineData("/wear/")]
    [InlineData("/catalogue/")]
    public async Task PublicApp_ShouldReportTheRenderingBuild_WhenRendered(string path)
    {
        await using var harness = new BlogHarness();
        var html = await harness.Client().GetStringAsync(path);
        var expected =
            typeof(SiteRenderer)
                .Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split(
                    '+')[0];
        html.Should().Contain("data-app-version");
        html.Should().Contain(">" + expected + "</small>");
    }
}
