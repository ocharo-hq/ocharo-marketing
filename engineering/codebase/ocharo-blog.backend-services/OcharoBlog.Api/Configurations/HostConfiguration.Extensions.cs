using Ocharo.Sso;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OcharoBlog.Api.Blog;
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
using WoW.Two.Sdk.Beta.Be.Media.Images;
using WoW.Two.Sdk.Beta.Be.Mediator;
using WoW.Two.Sdk.Beta.Be.Storage.Core;
using WoW.Two.Sdk.Beta.Be.Storage.FileSystem;
using WoW.Two.Sdk.Beta.Be.Web.ProblemDetails;

namespace OcharoBlog.Api.Configurations;

public static partial class HostConfiguration
{
    /// <summary>Registers the platform access-ticket client.</summary>
    private static WebApplicationBuilder AddIntegrationBoundary(this WebApplicationBuilder builder)
    {
        builder.Services.AddOcharoSso(builder.Configuration, "editorial", "ocharo-editorial", requireBoundOrganization: true);
        builder.Services.AddHttpClient("ocharo-platform-tickets", client => client.Timeout = TimeSpan.FromSeconds(5))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
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
        builder.Services.AddOptions<BlogOptions>().Bind(builder.Configuration.GetSection(BlogOptions.Section));
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
        builder.Services.AddHttpClient("ocharo-image-processing", client => client.Timeout = TimeSpan.FromSeconds(95))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false,
            });
        builder.Services.AddScoped<IEditorialImageWorker, EditorialImageWorker>();
        builder.Services.AddScoped<IMediaEditingService, MediaEditingService>();
        builder.Services.AddScoped<IMediaStore, MediaStore>();
        return builder;
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
        await scope.ServiceProvider.GetRequiredService<BlogSeeder>()
            .SeedIfEmptyAsync(app.Environment.ContentRootPath, CancellationToken.None);
        return app;
    }

    /// <summary>Configures the request boundaries and editorial SPA static files.</summary>
    private static WebApplication UsePipeline(this WebApplication app)
    {
        app.UseHttpProblemDetails();
        app.UseMiddleware<BlogExceptionMiddleware>();
        app.UseMiddleware<LocalPilotBoundaryMiddleware>();
        app.UseOcharoSso(http => http.Request.Path.StartsWithSegments("/api/public") || http.Request.Path == "/health" || http.Request.Path == "/api/system/status" || http.Request.Path == "/" || http.Request.Path.StartsWithSegments("/journal") || http.Request.Path.StartsWithSegments("/topics") || http.Request.Path.StartsWithSegments("/uz") || http.Request.Path.StartsWithSegments("/ru") || http.Request.Path.StartsWithSegments("/atlas") || http.Request.Path.StartsWithSegments("/wear") || http.Request.Path.StartsWithSegments("/catalogue") || http.Request.Path.StartsWithSegments("/media"),
            resolveSession: http => http.Request.Path.StartsWithSegments("/media") || http.Request.Path.StartsWithSegments("/preview"));
        app.UseMiddleware<IntegrationBoundaryMiddleware>();
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
        app.MapControllers();
        app.MapGet("/health", () => Results.Ok(new
        {
            status = "healthy",
            service = "OcharoBlog",
            mode = "local-pilot",
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
