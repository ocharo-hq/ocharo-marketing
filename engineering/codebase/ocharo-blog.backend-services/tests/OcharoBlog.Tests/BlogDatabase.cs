using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Npgsql;
using OcharoBlog.Api.Configurations;
using OcharoBlog.Persistence;
using Testcontainers.PostgreSql;
using WoW.Two.Sdk.Beta.Be.Data;
using WoW.Two.Sdk.Beta.Be.Data.Migrations.Bespoke;
using WoW.Two.Sdk.Beta.Be.Testing.Containers.Postgres;

namespace OcharoBlog.Tests;

/// <summary>
/// The PostgreSQL every Editorial test host runs on: one container per test run, a template database holding a stand-in
/// for Identity's set (<c>IdentityStub/</c>) with every test person and organization, then the <c>blog</c> and
/// <c>distributed_cache</c> sets the host's own <c>migrate</c> verb applied, and a copy of it per host, so each host
/// starts on its own current, empty schema. Testcontainers' reaper removes the container when the run ends.
/// </summary>
internal static class BlogDatabase
{
    /// <summary>The instance every test host claims the schemas as, whatever its environment, so a host with
    /// <c>DEPLOY_ENVIRONMENT</c> set boots on the template's claim.</summary>
    public const string OwnerInstance = "ocharo-blog-tests";

    private const string Template = "blog_template";

    private static readonly SemaphoreSlim Cloning = new(1, 1);
    private static readonly Lazy<Task<PostgresFixture>> Server = new(StartAsync);

    /// <summary>Creates a migrated database of its own and returns its connection string.</summary>
    /// <remarks>Blocks, for the synchronous test hosts' constructors; the work runs off the test's context.</remarks>
    public static string Create() => Task.Run(() => CreateAsync()).GetAwaiter().GetResult();

    /// <summary>Creates a database of its own, migrated unless <paramref name="empty"/>, and returns its connection
    /// string.</summary>
    public static async Task<string> CreateAsync(bool empty = false)
    {
        var server = await Server.Value;
        var name = $"blog_{Guid.NewGuid():N}";
        await Cloning.WaitAsync();
        try
        {
            await ExecuteAsync(server.ConnectionString,
                empty ? $"CREATE DATABASE {name}" : $"CREATE DATABASE {name} TEMPLATE {Template}");
        }
        finally
        {
            Cloning.Release();
        }
        return Connection(server.ConnectionString, name);
    }

    /// <summary>Drops a database <see cref="CreateAsync"/> made, closing what still holds it.</summary>
    public static async Task DropAsync(string connectionString)
    {
        var server = await Server.Value;
        var name = new NpgsqlConnectionStringBuilder(connectionString).Database;
        await ExecuteAsync(server.ConnectionString, $"DROP DATABASE IF EXISTS {name} WITH (FORCE)");
    }

    /// <summary>The settings that point a host at <paramref name="connectionString"/>.</summary>
    public static Dictionary<string, string?> Settings(string connectionString) => new()
    {
        ["DatabaseSettings:ConnectionString"] = connectionString,
        ["Migrations:OwnerInstance"] = OwnerInstance,
    };

    /// <summary>Runs the host's <c>migrate</c> verb over a database, as the deploy step does, with Identity's stand-in
    /// set beside Editorial's, and returns its exit code and output.</summary>
    /// <param name="connectionString">The database, handed over as <c>Migrations:ConnectionString</c>.</param>
    /// <param name="arguments">The verb's arguments after <c>migrate</c>; none for a bare <c>migrate</c>.</param>
    public static async Task<(int Exit, string Output, string Error)> MigrateAsync(string connectionString,
        params string[] arguments)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(Program).Assembly.GetName().Name,
            EnvironmentName = "Testing",
        });
        var settings = new Dictionary<string, string?>(EditorialSignIn.Settings)
        {
            ["Migrations:ConnectionString"] = connectionString,
        };
        foreach (var (key, value) in Settings(connectionString)) settings[key] = value;
        builder.Configuration.AddInMemoryCollection(settings);
        builder.Configure();
        builder.Services.AddDatabaseBespokeMigrationSet(BlogSchema.IdentitySet,
            Path.Combine(AppContext.BaseDirectory, "IdentityStub"), migration =>
            {
                migration.SchemaName = "identity";
                migration.SchemaOwner = "module";
            });
        await using var app = builder.Build();
        using var output = new StringWriter();
        using var error = new StringWriter();
        var exit = await MigrateCommand.RunAsync(app.Services, [MigrateCommand.Name, .. arguments], output, error);
        return (exit, output.ToString(), error.ToString());
    }

    private static async Task<PostgresFixture> StartAsync()
    {
        // Hosts run in parallel, each with its own pools, so the server takes more connections than its default.
        var server = new PostgresFixture(new PostgreSqlBuilder().WithImage(PostgresFixture.DefaultImage)
            .WithCommand("-c", "max_connections=500").Build());
        await server.StartAsync();
        await ExecuteAsync(server.ConnectionString, $"CREATE DATABASE {Template}");
        // No pooling, so no connection outlives the run and holds the template against its copies.
        var template = Connection(server.ConnectionString, Template) + ";Pooling=false";
        var (exit, output, error) = await MigrateAsync(template);
        if (exit != 0)
            throw new InvalidOperationException($"migrate exited {exit}: {error}{output}");
        await HoldIdentitiesAsync(template);
        return server;
    }

    /// <summary>Puts every test person and organization in Identity's stand-in tables, so a receipt may key into them.
    /// </summary>
    private static async Task HoldIdentitiesAsync(string connectionString)
    {
        var organizations = EditorialSignIn.Everyone.Select(person => person.OrganizationId).OfType<Guid>().Distinct();
        foreach (var organization in organizations)
            await ExecuteAsync(connectionString,
                "INSERT INTO identity.identity_organizations (id, name) VALUES ($1, 'Ocharo')", organization);
        foreach (var person in EditorialSignIn.Everyone)
            await ExecuteAsync(connectionString, "INSERT INTO identity.identity_users (id) VALUES ($1)",
                Guid.Parse(person.Subject));
    }

    private static string Connection(string server, string database) =>
        new NpgsqlConnectionStringBuilder(server) { Database = database }.ConnectionString;

    private static async Task ExecuteAsync(string connectionString, string sql, params object[] parameters)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var parameter in parameters)
            command.Parameters.Add(new NpgsqlParameter { Value = parameter });
        await command.ExecuteNonQueryAsync();
    }
}
