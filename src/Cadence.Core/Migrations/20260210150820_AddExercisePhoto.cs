using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddExercisePhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Observations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Complete");

            migrationBuilder.CreateTable(
                name: "ExercisePhotos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CapturedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ObservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BlobUri = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ThumbnailUri = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScenarioTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    LocationAccuracy = table.Column<double>(type: "float", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExercisePhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExercisePhotos_AspNetUsers_CapturedById",
                        column: x => x.CapturedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExercisePhotos_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExercisePhotos_Observations_ObservationId",
                        column: x => x.ObservationId,
                        principalTable: "Observations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExercisePhotos_CapturedById",
                table: "ExercisePhotos",
                column: "CapturedById");

            migrationBuilder.CreateIndex(
                name: "IX_ExercisePhotos_ExerciseId",
                table: "ExercisePhotos",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExercisePhotos_ExerciseId_CapturedAt",
                table: "ExercisePhotos",
                columns: new[] { "ExerciseId", "CapturedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ExercisePhotos_ObservationId",
                table: "ExercisePhotos",
                column: "ObservationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExercisePhotos");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Observations");
        }
    }
}
