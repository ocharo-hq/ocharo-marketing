using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OcharoMarketing.Persistence.Marketing.Migrations
{
    /// <inheritdoc />
    public partial class AssetFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileId",
                table: "Assets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExportFiles",
                columns: table => new
                {
                    DraftId = table.Column<string>(type: "TEXT", nullable: false),
                    Revision = table.Column<int>(type: "INTEGER", nullable: false),
                    Sha256 = table.Column<string>(type: "TEXT", nullable: false),
                    FileId = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportFiles", x => new { x.DraftId, x.Revision });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExportFiles");

            migrationBuilder.DropColumn(
                name: "FileId",
                table: "Assets");
        }
    }
}
