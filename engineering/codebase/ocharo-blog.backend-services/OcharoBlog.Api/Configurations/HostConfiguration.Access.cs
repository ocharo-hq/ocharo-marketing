using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Npgsql;
using OcharoBlog.Api.Access;
using OcharoBlog.Persistence;
using OcharoBlog.Persistence.Access;
using WoW.Two.Sdk.Beta.Be.Caching.Postgres;
using WoW.Two.Sdk.Beta.Be.Identity.Bff;
using WoW.Two.Sdk.Beta.Be.Identity.Jwt;
using WoW.Two.Sdk.Beta.Be.Identity.Signals;

namespace OcharoBlog.Api.Configurations;

public static partial class HostConfiguration
{
    /// <summary>The Data Protection application name every environment shares unless
    /// <c>DataProtection:ApplicationName</c> overrides it, so a restart opens the cookies the last boot
    /// sealed.</summary>
    private const string ApplicationName = "ocharo-editorial";

    /// <summary>Registers sign-in through Ocharo's Identity instance (BFF, section <c>Identity:Client</c>), the JWT
    /// check of service tokens that read live exports, and the session store and key ring the sessions survive a
    /// restart in.</summary>
    /// <remarks>The BFF keeps sessions in <c>IDistributedCache</c>: a table in schema <c>blog_cache</c> of the product
    /// database, whose <c>distributed_cache</c> set migrates beside the <c>blog</c> set. The key ring that seals the
    /// session cookie lives in <c>blog.data_protection_keys</c>, so a restart or deploy keeps editors signed
    /// in.</remarks>
    private static WebApplicationBuilder AddAccess(this WebApplicationBuilder builder)
    {
        // A verb that never serves, such as the release build's `migrate sets --json`, composes without the Identity
        // addresses; a serving boot without them refuses at start, naming the setting.
        var audience = HttpsAddress(builder.Configuration, ResourceUriKey);
        var issuer = HttpsAddress(builder.Configuration, AuthorityKey);
        if (audience is not null && issuer is not null)
        {
            builder.Services.AddJwtBearerAuthentication(options =>
            {
                options.Audience = audience;
                options.TrustedIssuers.Add(new TrustedIssuer { Issuer = issuer });
            });
        }
        else
        {
            builder.Services.AddOptions<IdentityAddresses>()
                .Validate(_ => false, $"{(audience is null ? ResourceUriKey : AuthorityKey)} {AddressRule}")
                .ValidateOnStart();
        }
        builder.Services.AddTokenDenyList();
        if (builder.Configuration[EditorialAccess.SignalUrlKey] is { Length: > 0 } signalUrl)
        {
            builder.Services.AddIdentitySignalReceiver(options =>
            {
                options.Audience = signalUrl;
                options.TrustedIssuers.Add(new TrustedIssuer
                    {
                        Issuer = RequiredAddress(builder.Configuration, "Identity:Client:Authority")
                    });
            });
        }
        builder.Services.AddIdentityBff();
        // Never Desk's `cache` schema. Resolved from the final configuration, after test and deployment overrides.
        builder.Services.AddPostgresDistributedCache(cache => cache.Schema = BlogSchema.CacheSchema);
        builder.Services.AddOptions<PostgresDistributedCacheOptions>()
            .Configure<IConfiguration>((cache, configuration) =>
                cache.ConnectionString = DatabaseConnection(configuration) ?? string.Empty);
        builder.Services.AddDataProtection();
        builder.Services.AddOptions<KeyManagementOptions>().Configure<NpgsqlDataSource>((keys, database) =>
            keys.XmlRepository = new PostgresXmlRepository(database));
        builder.Services.AddOptions<DataProtectionOptions>().Configure<IConfiguration>((protection, configuration) =>
            protection.ApplicationDiscriminator =
                configuration["DataProtection:ApplicationName"] is { Length: > 0 } name ? name : ApplicationName);
        return builder;
    }

    /// <summary>Reads a setting the service token check depends on, in the <c>Uri.AbsoluteUri</c> form Identity
    /// writes.</summary>
    /// <remarks>The JWT registration takes its issuer and audience while the host is built, so a test host hands them
    /// over
    /// with <c>UseSetting</c>; its own configuration arrives later.</remarks>
    private static string RequiredAddress(IConfiguration configuration, string key) =>
        HttpsAddress(configuration, key) ?? throw new InvalidOperationException($"{key} {AddressRule}");

    /// <summary>Reads an absolute HTTPS address in its <c>Uri.AbsoluteUri</c> form; null when the setting is missing or
    /// names another kind of address.</summary>
    private static string? HttpsAddress(IConfiguration configuration, string key) =>
        Uri.TryCreate(configuration[key], UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
            ? uri.AbsoluteUri
            : null;

    /// <summary>Holds the setting naming Editorial's audience for service tokens.</summary>
    private const string ResourceUriKey = "Identity:Resource:Uri";

    /// <summary>Holds the setting naming Ocharo's Identity instance, the one trusted issuer.</summary>
    private const string AuthorityKey = "Identity:Client:Authority";

    /// <summary>States what the Identity address settings must hold.</summary>
    private const string AddressRule = "must name an absolute HTTPS address.";

    /// <summary>Stands for the Identity addresses a serving boot checks when the host composed without them.</summary>
    private sealed class IdentityAddresses;

    /// <summary>Configures the session pipeline: HTTPS for the session routes, the BFF (authentication, the
    /// <c>X-Csrf-Token</c>
    /// check, authorization), then the bound-organization membership and permission check.</summary>
    private static IApplicationBuilder UseAccess(this IApplicationBuilder app)
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
        // authentication and authorization at the head of the pipeline, ahead of the proxy headers: they are set here
        // too.
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseWhen(context => !EditorialAccess.IsPublicApi(context.Request.Path), branch => branch.UseIdentityBff());
        app.UseMiddleware<EditorialAccessMiddleware>();
        return app;
    }
}
