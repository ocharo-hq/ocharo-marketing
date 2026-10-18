using Microsoft.EntityFrameworkCore;
using OcharoMarketing.Persistence;
using WoW.Two.Sdk.Beta.Be.Data.Migrations.Bespoke;
using WoW.Two.Sdk.Beta.Be.Testing.Data.EntityFrameworkCore;

namespace OcharoMarketing.Tests.Marketing;

/// <summary>Exercises the pilot's SQLite engine using the SDK relational harness.</summary>
public sealed class MarketingDatabase() : RelationalTestDb<OcharoMarketingDbContext>(DatabaseProvider.Sqlite)
{
    protected override OcharoMarketingDbContext CreateContext(DbContextOptionsBuilder<OcharoMarketingDbContext> builder) => new(builder.Options);
}
