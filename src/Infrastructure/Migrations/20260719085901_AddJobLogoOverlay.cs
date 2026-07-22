using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddJobLogoOverlay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LogoMargin",
                table: "DubbingJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LogoPosition",
                table: "DubbingJobs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "LogoScalePercent",
                table: "DubbingJobs",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "LogoStorageKey",
                table: "DubbingJobs",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoMargin",
                table: "DubbingJobs");

            migrationBuilder.DropColumn(
                name: "LogoPosition",
                table: "DubbingJobs");

            migrationBuilder.DropColumn(
                name: "LogoScalePercent",
                table: "DubbingJobs");

            migrationBuilder.DropColumn(
                name: "LogoStorageKey",
                table: "DubbingJobs");
        }
    }
}
