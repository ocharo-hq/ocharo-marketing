using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OcharoMarketing.Api.Configurations;
using OcharoMarketing.Infrastructure.Marketing;
using OcharoMarketing.Persistence;
using Testcontainers.PostgreSql;
using WoW.Two.Sdk.Beta.Be.Data;
using WoW.Two.Sdk.Beta.Be.Data.Abstractions;
using WoW.Two.Sdk.Beta.Be.Data.EntityFrameworkCore;
using WoW.Two.Sdk.Beta.Be.Data.Migrations.Bespoke;
using WoW.Two.Sdk.Beta.Be.Foundation.Results;
using WoW.Two.Sdk.Beta.Be.Meta;
using WoW.Two.Sdk.Beta.Be.Testing.Containers.Postgres;

namespace OcharoMarketing.Tests;

/// <summary>
/// One PostgreSQL 18 container for the test run (the be sdk's <see cref="PostgresFixture"/>), and a database per test
/// copied from a template that Marketing's own <c>migrate</c> verb brought current: the <c>marketing</c> and
/// <c>distributed_cache</c> sets exactly as a deploy applies them, never an EF-generated schema. Identity's set is a
/// stand-in (<c>IdentityStub/</c>) holding <see cref="Member"/> and the test organizations, as Studio's tests host it.
/// </summary>
/// <remarks>Each copy is dropped when its test ends; the reaper removes the container when the run ends.</remarks>
internal static class MarketingPostgres
{
    /// <summary>The instance every test host and verb claims the schemas as, the template's included.</summary>
    public const string OwnerInstance = "ocharo-marketing@test";

    /// <summary>An Identity account the stand-in set holds, for a job someone asked for.</summary>
    public static readonly Guid Member = Guid.Parse("5b0c6a8e-1f2d-4c3b-9a7e-6d5f4e3c2b1a");

    /// <summary>The organizations the stand-in set holds: the bound test organization and the outsider's.</summary>
    public static readonly Guid[] Organizations =
        [MarketingTestIdentity.Organization, MarketingTestIdentity.Outsider.OrganizationId!.Value];

    private const string Template = "marketing_template";
    private static readonly SemaphoreSlim Starting = new(1, 1);
    private static PostgresFixture? server;
    private static Exception? failure;

    /// <summary>Creates a database of its own at the current level of every set, and returns its connection
    /// string.</summary>
    public static async Task<string> CreateDatabaseAsync()
    {
        var root = await ServerAsync();
        var name = "m_" + Guid.NewGuid().ToString("N");
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await ExecuteAsync(root, $"CREATE DATABASE {name} TEMPLATE {Template}");
                return On(root, name);
            }
            catch (PostgresException exception)
                when (exception.SqlState == PostgresErrorCodes.ObjectInUse && attempt < 50)
            {
                // A session on the template, such as the migrate run closing, holds the copy back for a moment.
                await Task.Delay(100);
            }
        }
    }

    /// <summary>Names a database no set has touched yet, for a test of the <c>migrate</c> verb itself.</summary>
    public static async Task<string> EmptyDatabaseAsync() =>
        On(await ServerAsync(), "m_" + Guid.NewGuid().ToString("N"));

    /// <summary>Drops a test's database, ending any session still on it.</summary>
    public static async Task DropDatabaseAsync(string connectionString)
    {
        NpgsqlConnection.ClearPool(new NpgsqlConnection(connectionString));
        var name = new NpgsqlConnectionStringBuilder(connectionString).Database;
        await ExecuteAsync(await ServerAsync(), $"DROP DATABASE IF EXISTS {name} WITH (FORCE)");
    }

    /// <summary>An import gate over a store that does not exist: an environment with nothing to import.</summary>
    public static SqliteImportGate NoStore() => new(
        new SqliteImportSource(
            Path.Combine(Path.GetTempPath(), "ocharo-marketing-no-store", Guid.NewGuid().ToString("N") + ".db")),
        TimeProvider.System);

    /// <summary>Runs Marketing's own verb on the real host composition, as a deployed image does, and captures its
    /// streams.</summary>
    public static async Task<(int Exit, string Output, string Error)> RunVerbAsync(
        IReadOnlyDictionary<string, string?> settings, params string[] args)
    {
        await using var output = new StringWriter();
        await using var error = new StringWriter();
        var exit = await ServiceHost.RunAsync(args, builder =>
        {
            builder.Configuration.AddInMemoryCollection(settings);
            builder.Configure();
        }, _ => Task.CompletedTask, verbs => verbs.AddMarketingVerbs(), output, error, CancellationToken.None);
        return (exit, output.ToString(), error.ToString());
    }

    /// <summary>Starts the container once and migrates the template with the image's <c>migrate</c> verb.</summary>
    private static async Task<string> ServerAsync()
    {
        await Starting.WaitAsync();
        try
        {
            if (server is not null)
                return server.ConnectionString;
            // One failed start fails every later test at once, with no second container.
            if (failure is not null)
                throw new InvalidOperationException("The test PostgreSQL did not start.", failure);
            var container = new PostgreSqlBuilder().WithImage(PostgresFixture.DefaultImage)
                .WithCommand("-c", "fsync=off", "-c", "synchronous_commit=off", "-c", "max_connections=500").Build();
            var fixture = new PostgresFixture(container);
            try
            {
                await fixture.StartAsync();
                await ApplyIdentityStubAsync(On(fixture.ConnectionString, Template));
                var settings = MarketingTestIdentity.Settings();
                settings["Migrations:ConnectionString"] = On(fixture.ConnectionString, Template);
                settings["Marketing:DataDirectory"] = Path.Combine(Path.GetTempPath(), "ocharo-marketing-template");
                var (exit, output, error) = await RunVerbAsync(settings, "migrate");
                if (exit != 0)
                    throw new InvalidOperationException($"migrate exited {exit} on the template: {output} {error}");
            }
            catch (Exception exception)
            {
                failure = exception;
                await fixture.DisposeAsync();
                throw;
            }
            NpgsqlConnection.ClearAllPools();
            await ExecuteAsync(fixture.ConnectionString, "SELECT pg_terminate_backend(pid) FROM pg_stat_activity "
                + $"WHERE datname = '{Template}' AND pid <> pg_backend_pid()");
            server = fixture;
            return server.ConnectionString;
        }
        finally
        {
            Starting.Release();
        }
    }

    /// <summary>Creates the database when missing and applies the stand-in for Identity's set, holding
    /// <see cref="Member"/> and <see cref="Organizations"/>, as a product database holds Identity's before Marketing's
    /// step.</summary>
    public static async Task ApplyIdentityStubAsync(string connectionString)
    {
        var services = new ServiceCollection().AddLogging();
        var owner = new Dictionary<string, string?>
        {
            [MigrationConstants.OwnerInstanceConfigurationKey] = OwnerInstance,
        };
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(owner).Build());
        // Registering an instance leaves its disposal here: an open pooled session would hold back every copy.
        await using var dataSource = NpgsqlDataSource.Create(connectionString);
        services.AddSingleton(dataSource);
        services.AddKeyedSingleton<IDbConnectionFactory>(
            MarketingSchema.IdentitySet, (_, _) => new DataSourceConnectionFactory(dataSource));
        services.AddKeyedSingleton(
            MarketingSchema.IdentitySet, (_, _) => new DatabaseSettings { ConnectionString = connectionString });
        services.AddDatabaseBespokeMigrationSet(MarketingSchema.IdentitySet,
            Path.Combine(AppContext.BaseDirectory, "IdentityStub"), migration =>
            {
                migration.SchemaName = "identity";
                migration.SchemaOwner = "module";
            });
        await using (var provider = services.BuildServiceProvider())
        {
            var applied = await provider.MigrateBespokeForDeployAsync("test");
            if (applied.IsFailure(out var error, out _))
                throw new InvalidOperationException($"The Identity stand-in did not migrate: {error.Message}");
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var users = new NpgsqlCommand(
            "INSERT INTO identity.identity_users (id) VALUES ($1) ON CONFLICT (id) DO NOTHING", connection);
        users.Parameters.Add(new NpgsqlParameter { Value = Member });
        await users.ExecuteNonQueryAsync();
        await using var organizations = new NpgsqlCommand("INSERT INTO identity.identity_organizations (id) "
            + "SELECT unnest($1::uuid[]) ON CONFLICT (id) DO NOTHING", connection);
        organizations.Parameters.Add(new NpgsqlParameter { Value = Organizations });
        await organizations.ExecuteNonQueryAsync();
    }

    private static string On(string connectionString, string database) =>
        new NpgsqlConnectionStringBuilder(connectionString) { Database = database }.ConnectionString;

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}
