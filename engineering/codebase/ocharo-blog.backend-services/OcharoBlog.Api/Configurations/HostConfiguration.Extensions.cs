using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OcharoBlog.Api.Access;
using OcharoBlog.Api.Blog;
using OcharoBlog.Api.RuntimeConfig.Settings;
using OcharoBlog.Application;
using OcharoBlog.Application.Abstractions;
using OcharoBlog.Application.Blog.Repositories;
using OcharoBlog.Application.Blog.Services;
using OcharoBlog.Application.Blog.Settings;
using OcharoBlog.Application.Blog.Site;
using OcharoBlog.Infrastructure.Blog;
using OcharoBlog.Infrastructure.Time;
using OcharoBlog.Persistence;
using OcharoBlog.Persistence.Blog;
using WoW.Two.Sdk.Beta.Be.Foundation.Configuration;
using WoW.Two.Sdk.Beta.Be.Foundation.Options;
using WoW.Two.Sdk.Beta.Be.Files.Client;
using WoW.Two.Sdk.Beta.Be.Http.Auth.OAuth2ClientCredentials;
using WoW.Two.Sdk.Beta.Be.Identity.Bff;
using WoW.Two.Sdk.Beta.Be.Identity.Signals;
using WoW.Two.Sdk.Beta.Be.Media.Images;
using WoW.Two.Sdk.Beta.Be.Mediator;
using WoW.Two.Sdk.Beta.Be.Storage.Core;
using WoW.Two.Sdk.Beta.Be.Storage.FileSystem;
using WoW.Two.Sdk.Beta.Be.Web.Hosting;
using WoW.Two.Sdk.Beta.Be.Web.ProblemDetails;

namespace OcharoBlog.Api.Configurations;

public static partial class HostConfiguration
{
    /// <summary>Registers forwarded headers trusted only from loopback and <c>Deployment:TrustedProxies</c>.</summary>
    private static WebApplicationBuilder AddForwardedHeaders(this WebApplicationBuilder builder)
    {
        // Behind the TLS-terminating ingress, same-origin write checks need the forwarded scheme; any other sender keeps its own.
        var proxies = builder.Configuration.GetSection("Deployment:TrustedProxies").Get<string[]>() ?? [];
        builder.Services.AddProxyAwareHosting(options =>
        {
            foreach (var proxy in proxies)
                options.TrustedProxies.Add(proxy);
        });
        return builder;
    }

    /// <summary>Registers the public inquiry relay client.</summary>
    private static WebApplicationBuilder AddInquiries(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient("ocharo-inquiries", client => client.Timeout = TimeSpan.FromSeconds(10));
        return builder;
    }

    /// <summary>Registers SQLite with late configuration overrides and content-root-relative database files.</summary>
    private static WebApplicationBuilder AddSqliteDatabase(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<OcharoBlogDbContext>((provider, options) =>
        {
            var raw = (provider.GetService<IConfiguration>() ?? builder.Configuration)["ConnectionStrings:OcharoBlog"]
                ?? "Data Source=App_Data/ocharo-blog.db";
            var connection = new SqliteConnectionStringBuilder(raw);
            var contentRoot = provider.GetService<IHostEnvironment>()?.ContentRootPath;
            if (connection.DataSource is { Length: > 0 } file && file != ":memory:"
                && !Path.IsPathRooted(file) && contentRoot is not null)
            {
                connection.DataSource = Path.Combine(contentRoot, file);
            }

            options.UseSqlite(connection.ToString());
        });
        return builder;
    }

    /// <summary>Registers the shared system clock.</summary>
    private static WebApplicationBuilder AddClock(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IClock, SystemClock>();
        return builder;
    }

    /// <summary>Registers blog settings, records, editorial services, seeding and scheduled publishing.</summary>
    private static WebApplicationBuilder AddBlog(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<BlogOptions>().Bind(builder.Configuration.GetSection(BlogOptions.Section))
            .Validate(options => options.OrganizationId != Guid.Empty, "Blog:OrganizationId must name the Identity organization this Editorial instance belongs to.")
            .ValidateOnStart();
        builder.Services.AddScoped<IBlogRepository, BlogRepository>();
        builder.Services.AddScoped<ArticleService>();
        builder.Services.AddScoped<TopicService>();
        builder.Services.AddScoped<SiteSettingsService>();
        builder.Services.AddScoped<BlogSeeder>();
        builder.Services.AddScoped<EditorialStarterImporter>();
        if (builder.Configuration.GetValue("Blog:RunScheduler", true))
        {
            builder.Services.AddHostedService<ScheduledPublishingWorker>();
        }

        return builder;
    }

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

    /// <summary>Registers HTTP problem details, controllers and OpenAPI.</summary>
    private static WebApplicationBuilder AddControllers(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpProblemDetails();
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();
        return builder;
    }

    /// <summary>Configures the blog schema and seed records before requests are accepted.</summary>
    private static async Task<WebApplication> InitializeBlogAsync(this WebApplication app)
    {
        await app.Services.InitializeDatabaseAsync();
        using var scope = app.Services.CreateScope();
        if (UsesFiles(scope.ServiceProvider))
            await scope.ServiceProvider.BackfillFileIdsAsync(app.Logger);
        await scope.ServiceProvider.GetRequiredService<BlogSeeder>()
            .SeedIfEmptyAsync(app.Environment.ContentRootPath, CancellationToken.None);
        return app;
    }

    /// <summary>Configures the request boundaries and editorial SPA static files.</summary>
    private static WebApplication UsePipeline(this WebApplication app)
    {
        app.UseProxyAwareHosting();
        app.UseHttpProblemDetails();
        app.UseMiddleware<BlogExceptionMiddleware>();
        app.UseMiddleware<EditorialHttpBoundaryMiddleware>();
        app.UseAccess();
        if (app.Environment.IsLocalRun(app.Configuration))
        {
            app.MapOpenApi();
        }

        // The bare /studio redirect must run before endpoint matching ignores trailing slashes.
        app.Use((context, next) =>
        {
            if (context.Request.Path.Value == "/studio")
            {
                context.Response.Redirect("/studio/");
                return Task.CompletedTask;
            }

            return next(context);
        });
        app.UseStaticFiles();
        return app;
    }

    /// <summary>Configures controller, status, API error and editorial SPA endpoints.</summary>
    private static WebApplication MapEndpoints(this WebApplication app)
    {
        app.WarnAboutRejectedRuntimeConfig();
        app.MapControllers();
        app.MapIdentityBffEndpoints();
        if (!string.IsNullOrEmpty(app.Configuration[EditorialAccess.SignalUrlKey])) app.MapIdentitySignals(EditorialAccess.SignalPath);
        app.MapGet("/health", () => Results.Ok(new
        {
            status = "healthy",
            service = "OcharoBlog",
            version = typeof(HostConfiguration).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "dev",
        }));
        app.MapGet("/api/system/status", () => Results.Ok(new
        {
            status = "healthy",
            service = "OcharoBlog",
            version = typeof(HostConfiguration).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "dev",
        }));
        app.MapFallback("/api/{**slug}", () => Results.Problem(statusCode: 404, title: "API route not found"));
        app.MapFallbackToFile("/studio/{**path:nonfile}", "studio/index.html");
        return app;
    }
}
