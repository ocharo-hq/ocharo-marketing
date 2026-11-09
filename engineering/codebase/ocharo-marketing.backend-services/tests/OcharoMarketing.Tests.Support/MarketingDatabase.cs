using Microsoft.EntityFrameworkCore;
using OcharoMarketing.Persistence;
using Xunit;

namespace OcharoMarketing.Tests.Support;

/// <summary>A PostgreSQL database of the test's own, at the level of every set (<see cref="MarketingPostgres"/>), and
/// the contexts over it mapped as the host maps them.</summary>
public sealed class MarketingDatabase : IAsyncLifetime
{
    private string? connectionString;

    /// <summary>Gets the database's connection string.</summary>
    public string ConnectionString =>
        connectionString ?? throw new InvalidOperationException("Call InitializeAsync first.");

    public async Task InitializeAsync() => connectionString = await MarketingPostgres.CreateDatabaseAsync();

    /// <summary>A new context over the database, with the host's snake_case naming.</summary>
    public OcharoMarketingDbContext NewContext() => new(new DbContextOptionsBuilder<OcharoMarketingDbContext>()
        .UseNpgsql(ConnectionString).UseSnakeCaseNamingConvention().Options);

    public Task DisposeAsync() =>
        connectionString is null ? Task.CompletedTask : MarketingPostgres.DropDatabaseAsync(connectionString);
}
