using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OcharoMarketing.Persistence.Marketing.Migrations
{
    /// <inheritdoc />
    public partial class DeliveryPipeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeliveryConnections",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Revision = table.Column<int>(type: "INTEGER", nullable: false),
                    VerificationStatus = table.Column<string>(type: "TEXT", nullable: false),
                    SafeMessage = table.Column<string>(type: "TEXT", nullable: false),
                    ConfigurationStamp = table.Column<string>(type: "TEXT", nullable: false),
                    Json = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryConnections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MetricObservations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ScheduleId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    RecordedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    Json = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricObservations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MetricReadAttempts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Purpose = table.Column<string>(type: "TEXT", nullable: false),
                    ScheduleId = table.Column<string>(type: "TEXT", nullable: false),
                    AttemptedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    Json = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricReadAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PublicationOperations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ScheduleId = table.Column<string>(type: "TEXT", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", nullable: false),
                    ConnectionId = table.Column<string>(type: "TEXT", nullable: false),
                    ConnectionRevision = table.Column<int>(type: "INTEGER", nullable: false),
                    ConfigurationStamp = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    DueAt = table.Column<long>(type: "INTEGER", nullable: false),
                    ClaimToken = table.Column<string>(type: "TEXT", nullable: true),
                    LeaseExpiresAt = table.Column<long>(type: "INTEGER", nullable: true),
                    DispatchStartedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    Attempts = table.Column<int>(type: "INTEGER", nullable: false),
                    Json = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublicationOperations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SourceAnalyses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Revision = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Provider = table.Column<string>(type: "TEXT", nullable: false),
                    Model = table.Column<string>(type: "TEXT", nullable: true),
                    Locale = table.Column<string>(type: "TEXT", nullable: false),
                    Prompt = table.Column<string>(type: "TEXT", nullable: false),
                    SourcesJson = table.Column<string>(type: "TEXT", nullable: false),
                    RequestFingerprint = table.Column<string>(type: "TEXT", nullable: false),
                    SuggestedText = table.Column<string>(type: "TEXT", nullable: true),
                    ReviewedText = table.Column<string>(type: "TEXT", nullable: true),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceAnalyses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MetricObservations_ScheduleId_RecordedAt",
                table: "MetricObservations",
                columns: new[] { "ScheduleId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MetricReadAttempts_AttemptedAt",
                table: "MetricReadAttempts",
                column: "AttemptedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PublicationOperations_ScheduleId",
                table: "PublicationOperations",
                column: "ScheduleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicationOperations_Status_DueAt",
                table: "PublicationOperations",
                columns: new[] { "Status", "DueAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SourceAnalyses_Status",
                table: "SourceAnalyses",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryConnections");

            migrationBuilder.DropTable(
                name: "MetricObservations");

            migrationBuilder.DropTable(
                name: "MetricReadAttempts");

            migrationBuilder.DropTable(
                name: "PublicationOperations");

            migrationBuilder.DropTable(
                name: "SourceAnalyses");
        }
    }
}
