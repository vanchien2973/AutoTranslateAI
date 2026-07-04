using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DubbingJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceUrl = table.Column<string>(type: "text", nullable: true),
                    LocalFilePath = table.Column<string>(type: "text", nullable: true),
                    SourceLanguage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    AudioLanguage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SubtitleLanguage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    EnableDubbing = table.Column<bool>(type: "boolean", nullable: false),
                    VoiceGender = table.Column<int>(type: "integer", nullable: false),
                    BgmMode = table.Column<int>(type: "integer", nullable: false),
                    DuckingDb = table.Column<int>(type: "integer", nullable: false),
                    SubtitleMode = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CurrentStep = table.Column<int>(type: "integer", nullable: true),
                    ProgressPercent = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    OutputFilePath = table.Column<string>(type: "text", nullable: true),
                    WorkspacePath = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewReadyAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DubbingJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Phase = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    OutputPath = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobSteps_DubbingJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "DubbingJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Segments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobId = table.Column<Guid>(type: "uuid", nullable: false),
                    SegmentIndex = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<double>(type: "double precision", nullable: false),
                    EndTime = table.Column<double>(type: "double precision", nullable: false),
                    OriginalText = table.Column<string>(type: "text", nullable: false),
                    AudioTextAi = table.Column<string>(type: "text", nullable: true),
                    AudioTextEdited = table.Column<string>(type: "text", nullable: true),
                    SubtitleTextAi = table.Column<string>(type: "text", nullable: true),
                    SubtitleTextEdited = table.Column<string>(type: "text", nullable: true),
                    IsEdited = table.Column<bool>(type: "boolean", nullable: false),
                    SpeakerLabel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AssignedVoice = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TtsAudioPath = table.Column<string>(type: "text", nullable: true),
                    TtsDurationMs = table.Column<long>(type: "bigint", nullable: true),
                    NeedsTtsRegenerate = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Segments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Segments_DubbingJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "DubbingJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DubbingJobs_Status",
                table: "DubbingJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_JobSteps_JobId_StepType",
                table: "JobSteps",
                columns: new[] { "JobId", "StepType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Segments_JobId_SegmentIndex",
                table: "Segments",
                columns: new[] { "JobId", "SegmentIndex" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobSteps");

            migrationBuilder.DropTable(
                name: "Segments");

            migrationBuilder.DropTable(
                name: "DubbingJobs");
        }
    }
}
