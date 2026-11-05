using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OcharoMarketing.Persistence.Marketing.Migrations
{
    /// <inheritdoc />
    public partial class GenerationJobRequester : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequestedBySubject",
                table: "GenerationJobs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestedInOrganization",
                table: "GenerationJobs",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestedBySubject",
                table: "GenerationJobs");

            migrationBuilder.DropColumn(
                name: "RequestedInOrganization",
                table: "GenerationJobs");
        }
    }
}
