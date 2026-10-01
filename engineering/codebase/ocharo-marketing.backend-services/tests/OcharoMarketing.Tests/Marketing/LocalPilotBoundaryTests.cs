using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using OcharoMarketing.Api.Marketing;
using Xunit;

namespace OcharoMarketing.Tests.Marketing;

/// <summary>Verifies local browser boundaries before any product side effect.</summary>
public sealed class LocalPilotBoundaryTests
{
    [Theory]
    [InlineData("evil.example", "127.0.0.1", "", 403)]
    [InlineData("localhost:8277", "192.0.2.1", "", 403)]
    [InlineData("localhost:8277", "127.0.0.1", "https://evil.example", 403)]
    [InlineData("localhost:8277", "127.0.0.1", "http://localhost:8277", 204)]
    [InlineData("localhost:8277", "127.0.0.1", "https://localhost:8358", 204)]
    [InlineData("localhost:8277", "127.0.0.1", "https://localhost:8242", 403)]
    [InlineData("localhost:8277", "127.0.0.1", "http://127.0.0.1:8358", 204)]
    [InlineData("localhost:8277", "127.0.0.1", "https://localhost:8277", 403)]
    [InlineData("localhost:8277", "127.0.0.1", "https://localhost:8243", 403)]
    [InlineData("localhost:8277", "127.0.0.1", "https://localhost.evil.example:8358", 403)]
    [InlineData("localhost:8277", "127.0.0.1", "https://localhost:8358@evil.example", 403)]
    public async Task RejectsRemoteHostsAndCrossOriginWrites(string host, string remote, string origin, int expected)
    {
        using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Host = new HostString(host);
        context.Request.Method = "POST";
        context.Request.Scheme = "http";
        context.Connection.RemoteIpAddress = IPAddress.Parse(remote);
        if (origin.Length > 0) context.Request.Headers.Origin = origin;
        context.Response.Body = new MemoryStream();
        var boundary = new LocalPilotBoundaryMiddleware(ctx => { ctx.Response.StatusCode = 204; return Task.CompletedTask; });
        await boundary.InvokeAsync(context);
        Assert.Equal(expected, context.Response.StatusCode);
    }
}
