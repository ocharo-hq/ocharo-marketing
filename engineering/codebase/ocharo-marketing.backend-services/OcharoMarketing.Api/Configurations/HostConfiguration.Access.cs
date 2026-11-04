using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OcharoMarketing.Api.Access;
using OcharoMarketing.Infrastructure.Marketing;
using WoW.Two.Sdk.Beta.Be.Identity.Bff;

namespace OcharoMarketing.Api.Configurations;

public static partial class HostConfiguration
{
    /// <summary>Registers sign-in through Ocharo's Identity instance (BFF, section <c>Identity:Client</c>), the bound
    /// organization (<c>Marketing:OrganizationId</c>, required at boot) and the Data Protection key ring that encrypts the sessions.</summary>
    /// <remarks>The BFF keeps sessions in the SDK default, the in-memory distributed cache: Marketing stores in SQLite and no
    /// SQLite <c>IDistributedCache</c> exists, so a restart or deploy signs everyone out once. The key ring persists to
    /// <c>DataProtection:KeysPath</c>, else <c>{Marketing:DataDirectory}/keys</c>, beside the database on a deployed volume.</remarks>
    private static WebApplicationBuilder AddAccess(this WebApplicationBuilder builder)
    {
        builder.Services.AddIdentityBff();
        builder.Services.AddOptions<MarketingAccessOptions>().BindConfiguration(MarketingAccessOptions.Section)
            .Validate(options => options.OrganizationId != Guid.Empty, "Marketing:OrganizationId must name the Identity organization this Marketing instance serves.")
            .ValidateOnStart();
        // Resolve final configuration after test/deployment overrides have been applied.
        builder.Services.AddDataProtection();
        builder.Services.AddOptions<KeyManagementOptions>().Configure<IConfiguration, IOptions<MarketingOptions>>((options, configuration, marketing) =>
        {
            var path = configuration["DataProtection:KeysPath"];
            if (string.IsNullOrWhiteSpace(path)) path = Path.Combine(marketing.Value.DataDirectory, "keys");
            options.XmlRepository = new FileSystemXmlRepository(new DirectoryInfo(Path.GetFullPath(path)), NullLoggerFactory.Instance);
        });
        builder.Services.AddOptions<DataProtectionOptions>().Configure<IConfiguration, IHostEnvironment>((options, configuration, environment) =>
            options.ApplicationDiscriminator = configuration["DataProtection:ApplicationName"] ?? $"ocharo-marketing:{environment.EnvironmentName}");
        return builder;
    }

    /// <summary>Configures the session pipeline: HTTPS for the session routes, the BFF (authentication, the
    /// <c>X-Csrf-Token</c> check, authorization), then the bound-organization membership and permission check.</summary>
    private static WebApplication UseAccess(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/api/identity") && !context.Request.IsHttps)
            {
                await Results.Problem(statusCode: 400, title: "HTTPS is required",
                    extensions: new Dictionary<string, object?> { ["code"] = "https_required" }).ExecuteAsync(context);
                return;
            }
            await next(context);
        });
        // The BFF's antiforgery check answers 400 for any session's unsafe request under /api, and a visitor filling in the
        // public inquiry form has no token even when also signed in, so public routes skip the BFF. A branch's middleware does
        // not count for the host, which would then add authentication and authorization at the head of the pipeline, ahead of
        // the proxy headers: they are set here too.
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseWhen(context => !MarketingAccess.IsPublic(context.Request), branch => branch.UseIdentityBff());
        app.UseMiddleware<MarketingAccessMiddleware>();
        return app;
    }
}
