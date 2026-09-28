using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OcharoMarketing.Persistence.Marketing.Migrations;

/// <summary>Builds the local schema model without starting provider clients or application workers.</summary>
public sealed class MarketingDesignTimeFactory : IDesignTimeDbContextFactory<OcharoMarketingDbContext>
{
    public OcharoMarketingDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<OcharoMarketingDbContext>().UseSqlite("Data Source=marketing-design.db").Options);
}
