using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace OcharoMarketing.Persistence.Marketing.Migrations;

/// <summary>Creates the first durable local marketing schema.</summary>
[DbContext(typeof(OcharoMarketingDbContext))]
[Migration("202609270001_InitialMarketing")]
public sealed class InitialMarketing : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE "Products" ("Id" TEXT NOT NULL PRIMARY KEY, "Json" TEXT NOT NULL);
            CREATE TABLE "Metadata" ("Id" TEXT NOT NULL PRIMARY KEY, "Json" TEXT NOT NULL);
            CREATE TABLE "Assets" ("Id" TEXT NOT NULL PRIMARY KEY, "Json" TEXT NOT NULL, "BlobPath" TEXT NOT NULL);
            CREATE TABLE "Drafts" ("Id" TEXT NOT NULL PRIMARY KEY, "Revision" INTEGER NOT NULL, "ApprovedRevision" INTEGER NULL, "Json" TEXT NOT NULL);
            CREATE TABLE "DraftRevisions" ("Id" TEXT NOT NULL PRIMARY KEY, "DraftId" TEXT NOT NULL, "Revision" INTEGER NOT NULL, "Json" TEXT NOT NULL);
            CREATE TABLE "Schedules" ("Id" TEXT NOT NULL PRIMARY KEY, "DraftId" TEXT NOT NULL, "DraftRevision" INTEGER NOT NULL, "ScheduledAt" INTEGER NOT NULL, "Status" TEXT NOT NULL, "Json" TEXT NOT NULL);
            CREATE TABLE "GenerationJobs" ("Id" TEXT NOT NULL PRIMARY KEY, "Kind" TEXT NOT NULL, "Status" TEXT NOT NULL, "PayloadJson" TEXT NOT NULL, "Json" TEXT NOT NULL, "CreatedAt" INTEGER NOT NULL, "LeaseExpiresAt" INTEGER NULL, "ClaimToken" TEXT NULL, "Attempts" INTEGER NOT NULL, "CheckpointJson" TEXT NULL);
            CREATE UNIQUE INDEX "IX_DraftRevisions_DraftId_Revision" ON "DraftRevisions" ("DraftId", "Revision");
            CREATE INDEX "IX_Schedules_Status_ScheduledAt" ON "Schedules" ("Status", "ScheduledAt");
            CREATE INDEX "IX_GenerationJobs_Status_CreatedAt" ON "GenerationJobs" ("Status", "CreatedAt");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var table in new[] { "GenerationJobs", "Schedules", "DraftRevisions", "Drafts", "Assets", "Metadata", "Products" })
            migrationBuilder.DropTable(table);
    }
}
