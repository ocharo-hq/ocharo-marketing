using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Npgsql;
using OcharoMarketing.Api.Access;
using OcharoMarketing.Persistence;
using OcharoMarketing.Persistence.Access;
using WoW.Two.Sdk.Beta.Be.Caching.Postgres;
using WoW.Two.Sdk.Beta.Be.Foundation.Options;
using WoW.Two.Sdk.Beta.Be.Identity.Bff;

namespace OcharoMarketing.Api.Configurations;

public static partial class HostConfiguration
{
    /// <summary>Holds the Data Protection application name every Marketing environment shares unless
    /// <c>DataProtection:ApplicationName</c> names another; each environment keeps its own key ring in its own
    /// database.</summary>
    private const string DataProtectionApplication = "ocharo-marketing";

    /// <summary>Registers sign-in through Ocharo's Identity instance (BFF, section <c>Identity:Client</c>), the bound
    /// organization (<c>Marketing:OrganizationId</c>, required at boot and by <c>config check</c>), the sessions' store
    /// and the Data Protection key ring that encrypts them.</summary>
    /// <remarks>The BFF keeps sessions in <c>IDistributedCache</c>: the <c>marketing_cache.distributed_cache</c> table
    /// of the product database (be sdk set <c>distributed_cache</c>, applied beside Marketing's own). The key ring
    /// lives in
    /// <c>marketing.data_protection_keys</c> under a fixed application name, so a restart or deploy keeps a member
    /// signed in.</remarks>
    private static WebApplicationBuilder AddAccess(this WebApplicationBuilder builder)
    {
        builder.Services.AddIdentityBff();
        builder.Services.AddPostgresDistributedCache(cache => cache.Schema = MarketingSchema.CacheSchema);
        builder.Services.AddOptions<PostgresDistributedCacheOptions>().Configure<IConfiguration>(
            (cache, configuration) => cache.ConnectionString = ConnectionString(configuration) ?? string.Empty);
        builder.Services.AddValidatedSettings<MarketingAccessOptions>(
            builder.Configuration, MarketingAccessOptions.Section, rules => rules.ValidateSetting(
                nameof(MarketingAccessOptions.OrganizationId), "required",
                options => options.OrganizationId != Guid.Empty,
                "must name the Identity organization this Marketing instance serves"));
        // Resolve final configuration after test/deployment overrides have been applied.
        builder.Services.AddDataProtection();
        builder.Services.AddOptions<KeyManagementOptions>().Configure<NpgsqlDataSource>((options, dataSource) =>
            options.XmlRepository = new PostgresXmlRepository(dataSource));
        builder.Services.AddOptions<DataProtectionOptions>().Configure<IConfiguration>((options, configuration) =>
            options.ApplicationDiscriminator = configuration["DataProtection:ApplicationName"] is { Length: > 0 } name
                ? name
                : DataProtectionApplication);
        return builder;
    }

    /// <summary>Configures the session pipeline: HTTPS for the session routes, the BFF (authentication, the
    /// <c>X-Csrf-Token</c> check, authorization), then the bound-organization membership and permission
    /// check.</summary>
    private static IApplicationBuilder UseAccess(this IApplicationBuilder app)
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
        // The BFF's antiforgery check answers 400 for any session's unsafe request under /api, and a visitor filling in
        // the public inquiry form has no token even when also signed in, so public routes skip the BFF. A branch's
        // middleware does not count for the host, which would then add authentication and authorization at the head of
        // the pipeline, ahead of the proxy headers: they are set here too.
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseWhen(context => !MarketingAccess.IsPublic(context.Request), branch => branch.UseIdentityBff());
        app.UseMiddleware<MarketingAccessMiddleware>();
        return app;
    }
}
