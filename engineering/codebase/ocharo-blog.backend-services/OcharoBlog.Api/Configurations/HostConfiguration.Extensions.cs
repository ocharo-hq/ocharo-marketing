using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using OcharoBlog.Api.Access;
using OcharoBlog.Api.Blog;
using OcharoBlog.Api.RuntimeConfig.Settings;
using OcharoBlog.Application;
using OcharoBlog.Application.Blog.Repositories;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Application.Blog.Settings;
using OcharoBlog.Application.Blog.Site;
using OcharoBlog.Infrastructure.Blog;
using OcharoBlog.Persistence;
using OcharoBlog.Persistence.Blog;
using OcharoBlog.Persistence.Import;
using WoW.Two.Sdk.Beta.Be.Data.Abstractions;
using WoW.Two.Sdk.Beta.Be.Data.EntityFrameworkCore;
using WoW.Two.Sdk.Beta.Be.Data.EntityFrameworkCore.Postgres;
using WoW.Two.Sdk.Beta.Be.Data.Migrations.Bespoke;
using WoW.Two.Sdk.Beta.Be.Files.Client;
using WoW.Two.Sdk.Beta.Be.Foundation.Configuration.ConfigCheck;
using WoW.Two.Sdk.Beta.Be.Foundation.Options;
using WoW.Two.Sdk.Beta.Be.Http.Auth.OAuth2ClientCredentials;
using WoW.Two.Sdk.Beta.Be.Identity.Bff;
using WoW.Two.Sdk.Beta.Be.Identity.Signals;
using WoW.Two.Sdk.Beta.Be.Jobs.Core;
using WoW.Two.Sdk.Beta.Be.Media.Images;
using WoW.Two.Sdk.Beta.Be.Mediator;
using WoW.Two.Sdk.Beta.Be.Meta;
using WoW.Two.Sdk.Beta.Be.Observability.HealthChecks;
using WoW.Two.Sdk.Beta.Be.Storage.Core;
using WoW.Two.Sdk.Beta.Be.Storage.FileSystem;
using WoW.Two.Sdk.Beta.Be.Web.Hosting;
using WoW.Two.Sdk.Beta.Be.Web.Json;
using WoW.Two.Sdk.Beta.Be.Web.ProblemDetails;

namespace OcharoBlog.Api.Configurations;

public static partial class HostConfiguration
{
    /// <summary>Names the service in telemetry and in the <c>config check</c> result.</summary>
    private const string ServiceName = "ocharo-blog";

    /// <summary>Registers the SDK boot bundle and the HTTP problem-details boundary: forwarded headers trusted
    /// only from loopback and <c>Deployment:TrustedProxies</c>, the Studio served under <c>/studio/</c>, and the
    /// public pages free to embed images from WoW2 Files, Catalogue and Brand.</summary>
    private static WebApplicationBuilder AddHostDefaults(this WebApplicationBuilder builder)
    {
        builder.AddApiDefaults(options =>
        {
            options.ServiceName = ServiceName;
            // Behind the TLS-terminating ingress, same-origin write checks need the forwarded scheme.
            options.TrustProxiesFrom(builder.Configuration.GetSection("Deployment"));
            // Public pages and previews show cross-origin images that send no CORP opt-in; other origins embed /media.
            options.EnableCrossOriginEmbedderPolicy = false;
            options.CrossOriginEmbeddingPaths.Add("/media");
            options.SpaHosting = ConfigureStudioHosting;
        });
        builder.Services.AddHttpProblemDetails();
        return builder;
    }

    /// <summary>Configures the Studio's bundle in <c>wwwroot/studio/</c>: a bare <c>/studio</c> redirects to
    /// <c>/studio/</c>, its hashed assets cache for a year, and an unmatched page falls back to its shell.</summary>
    private static void ConfigureStudioHosting(SpaHostingOptions studio)
    {
        studio.FallbackFile = "studio/index.html";
        studio.ImmutableAssetsPath = "/studio/assets";
        studio.RedirectToTrailingSlash = true;
    }

    /// <summary>Registers the public inquiry relay client.</summary>
    private static WebApplicationBuilder AddInquiries(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient("ocharo-inquiries", client => client.Timeout = TimeSpan.FromSeconds(10));
        return builder;
    }

    /// <summary>Holds the key naming the SQLite store an earlier release kept, which <c>import-sqlite</c> copies once;
    /// the image sets <c>Data Source=/data/ocharo-blog.db</c>.</summary>
    private const string SqliteStoreKey = "ConnectionStrings:OcharoBlog";

    /// <summary>Registers PostgreSQL over <c>DatabaseSettings:ConnectionString</c> (<c>DB_CONNECTION</c> wins) with EF
    /// as the pure mapper of the <c>blog</c> set, its readiness check on <c>/health</c>, and the import of the SQLite
    /// store an earlier release kept, with the gate that holds writes back until it ran.</summary>
    private static WebApplicationBuilder AddPostgresDatabase(this WebApplicationBuilder builder)
    {
        // Resolved late, so a test host's settings win; a migrate run connects every set through
        // Migrations:ConnectionString.
        builder.Services.AddSingleton(provider => new DatabaseSettings
        {
            ConnectionString = DatabaseConnection(provider.GetRequiredService<IConfiguration>())
                ?? throw new InvalidOperationException("Set DatabaseSettings:ConnectionString or DB_CONNECTION."),
        });
        builder.Services.AddNpgsqlDataSource();
        builder.Services.AddDataSourceConnectionFactory();
        builder.Services.AddDbContext<OcharoBlogDbContext>((provider, options) => options
            .UseNpgsql(provider.GetRequiredService<NpgsqlDataSource>())
            .UseSnakeCaseNamingConvention());
        // The named set owns schema `blog` of the product database Desk and Identity share; the default set's history
        // in `public` is Desk's, so Editorial registers none.
        builder.Services.AddDatabaseBespokeMigrationSet(BlogSchema.MigrationSet, typeof(OcharoBlogDbContext).Assembly,
            migration =>
            {
                migration.SchemaName = BlogSchema.Name;
                migration.SchemaOwner = BlogSchema.Owner;
                migration.Version = MigrationVersionMapper.ToVersion(typeof(HostConfiguration).Assembly);
                // The assembly version, never the informational one: a local build's is `dev`, which names no release.
                migration.Release = typeof(OcharoBlogDbContext).Assembly.GetName().Version?.ToString(3);
                // The image receipts key into Identity's organizations and accounts: its set applies first, and the
                // step refuses before any SQL while it is missing or below the level that publishes them.
                migration.DependsOn(BlogSchema.IdentitySet, BlogSchema.IdentityLevel);
            });
        builder.Services.AddHealthChecks().AddDatabaseReadinessCheck<OcharoBlogDbContext>();
        builder.Services.AddSingleton(provider => new SqliteImportGate(SqliteStorePath(provider)));
        builder.Services.AddScoped<SqliteImporter>();
        return builder;
    }

    /// <summary>Reads the database connection: <c>DB_CONNECTION</c>, else <c>DatabaseSettings:ConnectionString</c>;
    /// null when both are blank.</summary>
    private static string? DatabaseConnection(IConfiguration configuration) =>
        new[] { configuration["DB_CONNECTION"], configuration["DatabaseSettings:ConnectionString"] }
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    /// <summary>Resolves the SQLite store's full path from <see cref="SqliteStoreKey"/>, relative to the content root;
    /// null for a blank or in-memory one.</summary>
    private static string? SqliteStorePath(IServiceProvider provider)
    {
        var raw = provider.GetRequiredService<IConfiguration>()[SqliteStoreKey]
            ?? "Data Source=App_Data/ocharo-blog.db";
        var file = new SqliteConnectionStringBuilder(raw).DataSource;
        return string.IsNullOrWhiteSpace(file) || file == ":memory:"
            ? null
            : Path.GetFullPath(file, provider.GetRequiredService<IHostEnvironment>().ContentRootPath);
    }

    /// <summary>Registers blog settings, records, editorial services, seeding and scheduled publishing.</summary>
    private static WebApplicationBuilder AddBlog(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddSettingsRoot(BlogOptions.Section)
            .AddValidatedSettings<BlogOptions>(builder.Configuration, BlogOptions.Section, ValidateBlog);
        builder.Services.AddScoped<IBlogRepository, BlogRepository>();
        builder.Services.AddScoped<ArticleService>();
        builder.Services.AddScoped<TopicService>();
        builder.Services.AddScoped<SiteSettingsService>();
        builder.Services.AddScoped<BlogSeeder>();
        builder.Services.AddScoped<EditorialStarterImporter>();
        builder.Services.AddScoped<ScheduledPublisher>();
        builder.Services.AddPeriodicJob<ScheduledPublisher>(
            "scheduled-publishing",
            (publisher, ct) => publisher.PublishDueAsync(ct),
            (job, services) =>
                job.Interval = TimeSpan.FromSeconds(services.GetRequiredService<BlogOptions>().SchedulerSeconds));
        return builder;
    }

    /// <summary>States the rules <c>Blog</c> settings must satisfy at boot and at <c>config check</c>.</summary>
    private static void ValidateBlog(OptionsBuilder<BlogOptions> blog) => blog
        .ValidateSetting(nameof(BlogOptions.OrganizationId), "required",
            options => options.OrganizationId != Guid.Empty,
            "must name the Identity organization this Editorial instance belongs to")
        .ValidateSetting(nameof(BlogOptions.DataRoot), "required",
            options => !string.IsNullOrWhiteSpace(options.DataRoot),
            "must name the folder for blobs and keys")
        .ValidateSetting(nameof(BlogOptions.SchedulerSeconds), "range",
            options => options.SchedulerSeconds == 0 || options.SchedulerSeconds >= BlogOptions.MinimumSchedulerSeconds,
            $"must be 0 (off) or at least {BlogOptions.MinimumSchedulerSeconds}")
        .ValidateSetting(nameof(BlogOptions.PreviewSize), "range",
            options => options.PreviewSize > 0, "must be positive")
        .ValidateSetting(nameof(BlogOptions.MaximumImageBytes), "range",
            options => options.MaximumImageBytes > 0, "must be positive")
        .ValidateSetting(nameof(BlogOptions.TrustedOrigins), "origin",
            options => options.TrustedOrigins.All(BlogOptions.IsHttpsOrigin),
            "each entry must be an HTTPS origin: scheme, host and an optional port, with no path or trailing slash");

    /// <summary>Registers blob-backed media, image processing and editorial image adapters.</summary>
    private static WebApplicationBuilder AddMedia(this WebApplicationBuilder builder)
    {
        var contentRoot = builder.Environment.ContentRootPath;
        // Resolve late so test-host configuration overrides continue to win.
        builder.Services.AddSingleton<IBlobRepository>(provider =>
        {
            var dataRoot = provider.GetRequiredService<IOptions<BlogOptions>>().Value.DataRoot;
            var root = Path.IsPathRooted(dataRoot) ? dataRoot : Path.Combine(contentRoot, dataRoot);
            return new LocalFileBlobRepository(Path.Combine(root, "blobs"));
        });
        builder.Services.AddImageProcessing(options =>
        {
            options.MaxInputBytes = MediaEditingService.MaximumBytes;
            options.MaxPixels = MediaEditingService.MaximumPixels;
            options.MaxOutputDimension = MediaEditingService.MaximumDimension;
        });
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHttpClient(EditorialImageWorker.ClientName, client => client.Timeout = EditorialImageWorker.RemovalDeadline)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false,
            })
            .AddOAuth2ClientCredentials(_ => { });
        // Resolve late so test-host configuration overrides continue to win. Editorial calls as its own client; without a
        // usable Blog:DeskBaseUrl the worker makes no call, so nothing here may fail the boot.
        builder.Services.AddOptions<OAuth2ClientCredentialsOptions>(EditorialImageWorker.ClientName)
            .Configure<IConfiguration>((options, configuration) =>
            {
                options.TokenEndpoint = Uri.TryCreate($"{configuration["Identity:Client:Authority"]?.TrimEnd('/')}/connect/token", UriKind.Absolute, out var endpoint) ? endpoint : null;
                options.ClientId = configuration["Identity:Client:ClientId"] ?? "";
                options.ClientSecret = configuration["Identity:Client:ClientSecret"] ?? "";
                options.Resource = EditorialImageWorker.DeskOrigin(configuration["Blog:DeskBaseUrl"])?.AbsoluteUri;
            });
        builder.Services.AddScoped<IEditorialImageWorker, EditorialImageWorker>();
        builder.Services.AddScoped<IMediaEditingService, MediaEditingService>();
        builder.AddEditorialFiles();
        return builder;
    }

    /// <summary>Holds the root URL of the WoW2 Files instance Editorial stores its media in; unset, media stay in the local
    /// blob folder.</summary>
    private const string FilesBaseUrlKey = "Files:BaseUrl";

    /// <summary>Registers the media store: WoW2 Files over HTTP when <see cref="FilesBaseUrlKey"/> is set, by tokens of
    /// Editorial's own Identity client (a person's exchanged within their BFF session, Editorial's own otherwise), else the
    /// local blob folder. Both read at resolve time, so a test host's settings decide.</summary>
    private static WebApplicationBuilder AddEditorialFiles(this WebApplicationBuilder builder)
    {
        builder.Services.AddFilesClient(_ => { });
        builder.Services.AddOptions<FilesClientOptions>().Configure<IConfiguration>((files, configuration) =>
        {
            if (configuration[FilesBaseUrlKey] is not { Length: > 0 } url)
                return;
            files.BaseAddress = new Uri(url);
            if (!Uri.TryCreate(configuration["Identity:Client:Authority"], UriKind.Absolute, out var authority))
                return;
            files.Identity = new FilesClientIdentityOptions
            {
                TokenEndpoint = new Uri($"{authority.AbsoluteUri.TrimEnd('/')}/connect/token"),
                ClientId = configuration["Identity:Client:ClientId"] ?? string.Empty,
                ClientSecret = configuration["Identity:Client:ClientSecret"] ?? string.Empty,
            };
        });
        builder.Services.AddScoped<MediaStore>();
        builder.Services.AddScoped<FilesMediaStore>();
        builder.Services.AddScoped<FilesMediaBackfill>();
        builder.Services.AddScoped<IMediaStore>(provider => UsesFiles(provider)
            ? provider.GetRequiredService<FilesMediaStore>()
            : provider.GetRequiredService<MediaStore>());
        return builder;
    }

    /// <summary>Tells whether this run stores media in WoW2 Files.</summary>
    private static bool UsesFiles(IServiceProvider provider) =>
        provider.GetRequiredService<IConfiguration>()[FilesBaseUrlKey] is { Length: > 0 };

    /// <summary>Fills the file ids the migration made; an unreachable Files leaves those files on local bytes, never the boot.</summary>
    private static async Task BackfillFileIdsAsync(this IServiceProvider services, ILogger logger)
    {
        try
        {
            await services.GetRequiredService<FilesMediaBackfill>().RunAsync(CancellationToken.None);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            logger.LogWarning(exception, "Editorial's file ids stay unfilled: WoW2 Files could not be reached at boot.");
        }
    }

    /// <summary>Registers catalogue publication settings and the catalogue feed client.</summary>
    private static WebApplicationBuilder AddCatalogue(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(provider => new PublicationSiteOptions
        {
            CatalogueAdminUrl = provider.GetRequiredService<IOptions<BlogOptions>>().Value.CatalogueAdminUrl,
        });
        builder.Services.AddHttpClient<ICatalogueFeed, CatalogueFeed>(
            client => client.Timeout = TimeSpan.FromSeconds(8));
        return builder;
    }

    /// <summary>Registers the canonical brand kit client.</summary>
    private static WebApplicationBuilder AddBrand(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient<IBrandKit, BrandKit>(client => client.Timeout = TimeSpan.FromSeconds(5));
        builder.Services.AddMemoryCache();
        return builder;
    }

    /// <summary>Registers the public and preview site services.</summary>
    private static WebApplicationBuilder AddSites(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<SiteService>();
        builder.Services.AddScoped<PublicationSiteService>();
        return builder;
    }

    /// <summary>Registers the mediator against the application assembly.</summary>
    private static WebApplicationBuilder AddMediator(this WebApplicationBuilder builder)
    {
        builder.Services.AddMediator(typeof(ApplicationAssembly).Assembly);
        return builder;
    }

    /// <summary>Registers the public runtime settings the SPA reads at startup: the Ocharo app and site addresses.</summary>
    private static WebApplicationBuilder AddRuntimeConfig(this WebApplicationBuilder builder)
    {
        // Every address is optional, and a malformed one is left out rather than failing the host over a menu link.
        builder.Services.AddValidatedSettings<OcharoAppsSettings>(builder.Configuration, OcharoAppsSettings.SectionName, _ => { });
        return builder;
    }

    /// <summary>Warns once about each configured app address the runtime config leaves out.</summary>
    private static void WarnAboutRejectedRuntimeConfig(this WebApplication app)
    {
        var rejected = app.Services.GetRequiredService<OcharoAppsSettings>().RejectedSettings();
        if (rejected.Count > 0)
            app.Logger.LogWarning("Runtime config leaves out {Settings}: each must be a plain HTTPS address.", string.Join(", ", rejected));
    }

    /// <summary>Registers the controllers with the SDK wire JSON.</summary>
    private static WebApplicationBuilder AddControllers(this WebApplicationBuilder builder)
    {
        builder.Services.AddControllersWithSdkJson();
        return builder;
    }

    /// <summary>Fills Files ids and seeds an empty blog before requests are accepted; both skip while the SQLite store
    /// waits for its import.</summary>
    private static async Task<WebApplication> InitializeBlogAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        if (UsesFiles(scope.ServiceProvider))
            await scope.ServiceProvider.BackfillFileIdsAsync(app.Logger);
        await scope.ServiceProvider.GetRequiredService<BlogSeeder>()
            .SeedIfEmptyAsync(app.Environment.ContentRootPath, CancellationToken.None);
        return app;
    }

    /// <summary>Configures the problem-details boundary, then the SDK pipeline with Editorial's boundary after
    /// routing.</summary>
    private static WebApplication UsePipeline(this WebApplication app)
    {
        // Outermost: every error status, MVC error result and thrown exception leaves as a problem document.
        app.UseHttpProblemDetails();
        // Forwarded headers, secure headers and compression, the Studio's files before routing; then blog errors,
        // Editorial's response headers and cross-origin write check, sign-in and access, ahead of the endpoints.
        app.UseApiDefaults(pipeline => pipeline.UseEditorialBoundary());
        return app;
    }

    /// <summary>Configures the pending-import refusal, blog errors as problems, Editorial's response headers and
    /// cross-origin write check, then sign-in and access.</summary>
    private static void UseEditorialBoundary(this IApplicationBuilder pipeline)
    {
        pipeline.UseMiddleware<SqliteImportPendingMiddleware>();
        pipeline.UseMiddleware<BlogExceptionMiddleware>();
        pipeline.UseMiddleware<EditorialHttpBoundaryMiddleware>();
        pipeline.UseAccess();
    }

    /// <summary>Matches a page outside <c>/studio/</c> and <c>/api/</c>: the public site's misses.</summary>
    private const string PublicPageMiss = "/{**path:regex(^(?!(studio|api)(/|$)).+)}";

    /// <summary>Configures controller, status, Identity and public-miss endpoints.</summary>
    private static WebApplication MapEndpoints(this WebApplication app)
    {
        app.WarnAboutRejectedRuntimeConfig();
        app.MapControllers();
        app.MapIdentityBffEndpoints();
        if (!string.IsNullOrEmpty(app.Configuration[EditorialAccess.SignalUrlKey])) app.MapIdentitySignals(EditorialAccess.SignalPath);
        app.MapGet("/api/system/status", () => Results.Ok(new
        {
            status = "healthy",
            service = "OcharoBlog",
            version = typeof(HostConfiguration).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "dev",
        }));
        // The SDK falls back to the Studio's shell for every unmatched page and has no path prefix for it yet; the
        // public site's misses stay 404s, outranking that fallback.
        app.MapMethods(PublicPageMiss, [HttpMethods.Get, HttpMethods.Head], () => Results.NotFound())
            .ExcludeFromDescription();
        return app;
    }
}
