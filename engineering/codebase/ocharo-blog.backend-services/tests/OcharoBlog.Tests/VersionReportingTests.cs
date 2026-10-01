using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using OcharoBlog.Api.Configurations;
using OcharoBlog.Application.Blog.Site;
namespace OcharoBlog.Tests;
public sealed class VersionReportingTests
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/api/system/status")]
    public async Task Status_reports_the_actual_host_assembly_version(string path)
    {
        await using var harness = new BlogHarness();
        var result = await harness.Client().GetFromJsonAsync<JsonElement>(path);
        var expected = typeof(HostConfiguration).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
        Assert.Equal(expected, result.GetProperty("version").GetString());
    }
    [Theory]
    [InlineData("/")]
    [InlineData("/atlas/")]
    [InlineData("/wear/")]
    [InlineData("/catalogue/")]
    public async Task Every_public_app_reports_the_rendering_build(string path)
    {
        await using var harness = new BlogHarness();
        var html = await harness.Client().GetStringAsync(path);
        var expected = typeof(SiteRenderer).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];
        Assert.Contains("data-app-version", html);
        Assert.Contains(">" + expected + "</small>", html);
    }
}
