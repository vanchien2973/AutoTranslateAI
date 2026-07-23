using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubtitleStyle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceMediaFileName",
                table: "DubbingJobs",
                type: "character varying(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SubtitleBold",
                table: "DubbingJobs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SubtitleFontFamily",
                table: "DubbingJobs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubtitleFontSize",
                table: "DubbingJobs",
                type: "integer",
                nullable: false,
                // Backfill existing rows with the domain default (24); 0 would be below the valid minimum.
                defaultValue: 24);

            migrationBuilder.AddColumn<bool>(
                name: "SubtitleItalic",
                table: "DubbingJobs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SubtitlePosition",
                table: "DubbingJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceMediaFileName",
                table: "DubbingJobs");

            migrationBuilder.DropColumn(
                name: "SubtitleBold",
                table: "DubbingJobs");

            migrationBuilder.DropColumn(
                name: "SubtitleFontFamily",
                table: "DubbingJobs");

            migrationBuilder.DropColumn(
                name: "SubtitleFontSize",
                table: "DubbingJobs");

            migrationBuilder.DropColumn(
                name: "SubtitleItalic",
                table: "DubbingJobs");

            migrationBuilder.DropColumn(
                name: "SubtitlePosition",
                table: "DubbingJobs");
        }
    }
}
