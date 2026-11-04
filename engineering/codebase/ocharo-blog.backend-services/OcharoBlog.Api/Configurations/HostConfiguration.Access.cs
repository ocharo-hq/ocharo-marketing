using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OcharoBlog.Api.Access;
using OcharoBlog.Infrastructure.Blog;
using WoW.Two.Sdk.Beta.Be.Identity.Bff;

namespace OcharoBlog.Api.Configurations;

public static partial class HostConfiguration
{
    /// <summary>Registers sign-in through Ocharo's Identity instance (BFF, section <c>Identity:Client</c>) and the Data
    /// Protection key ring that encrypts the sessions.</summary>
    /// <remarks>The BFF keeps sessions in the SDK default, the in-memory distributed cache: Editorial stores in SQLite and no
    /// SQLite <c>IDistributedCache</c> exists, so a restart or deploy signs everyone out once. The key ring persists at
    /// <c>DataProtection:KeysPath</c>, else <c>{Blog:DataRoot}/keys</c>, which a deployed host keeps on its data volume.</remarks>
    private static WebApplicationBuilder AddAccess(this WebApplicationBuilder builder)
    {
        builder.Services.AddIdentityBff();
        // Resolve final configuration after test/deployment overrides have been applied.
        builder.Services.AddDataProtection();
        builder.Services.AddOptions<KeyManagementOptions>()
            .Configure<IConfiguration, IHostEnvironment, IOptions<BlogOptions>>((options, configuration, environment, blog) =>
            {
                var path = configuration["DataProtection:KeysPath"];
                if (string.IsNullOrWhiteSpace(path)) path = Path.Combine(blog.Value.DataRoot, "keys");
                if (!Path.IsPathRooted(path)) path = Path.Combine(environment.ContentRootPath, path);
                options.XmlRepository = new FileSystemXmlRepository(new DirectoryInfo(path), NullLoggerFactory.Instance);
            });
        builder.Services.AddOptions<DataProtectionOptions>().Configure<IConfiguration, IHostEnvironment>((options, configuration, environment) =>
            options.ApplicationDiscriminator = configuration["DataProtection:ApplicationName"] ?? $"ocharo-editorial:{environment.EnvironmentName}");
        return builder;
    }

    /// <summary>Configures the session pipeline: HTTPS for the session routes, the BFF (authentication, the <c>X-Csrf-Token</c>
    /// check, authorization), then the bound-organization membership and permission check.</summary>
    private static WebApplication UseAccess(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            // Sign-in needs the secure cookie; a plaintext request would end in a session the browser drops.
            if (context.Request.Path.StartsWithSegments("/api/identity") && !context.Request.IsHttps)
            {
                await Results.Problem(statusCode: 400, title: "HTTPS is required",
                    extensions: new Dictionary<string, object?> { ["code"] = "https_required" }).ExecuteAsync(context);
                return;
            }
            await next(context);
        });
        // The BFF's antiforgery check answers 400 for any session's unsafe request under /api, so the public API, which
        // anyone may post to, skips the BFF. A branch's middleware does not count for the host, which would then add
        // authentication and authorization at the head of the pipeline, ahead of the proxy headers: they are set here too.
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseWhen(context => !EditorialAccess.IsPublicApi(context.Request.Path), branch => branch.UseIdentityBff());
        app.UseMiddleware<EditorialAccessMiddleware>();
        return app;
    }
}
