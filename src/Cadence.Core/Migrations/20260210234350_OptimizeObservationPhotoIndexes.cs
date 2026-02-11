using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeObservationPhotoIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Observations_ExerciseId_IsDeleted_ObservedAt",
                table: "Observations",
                columns: new[] { "ExerciseId", "IsDeleted", "ObservedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ExercisePhotos_ObservationId_IsDeleted_DisplayOrder",
                table: "ExercisePhotos",
                columns: new[] { "ObservationId", "IsDeleted", "DisplayOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Observations_ExerciseId_IsDeleted_ObservedAt",
                table: "Observations");

            migrationBuilder.DropIndex(
                name: "IX_ExercisePhotos_ObservationId_IsDeleted_DisplayOrder",
                table: "ExercisePhotos");
        }
    }
}
