using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class FixExercisePhotoConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ExercisePhotos_OrganizationId",
                table: "ExercisePhotos",
                column: "OrganizationId");

            // IX_ExercisePhotos_ObservationId_IsDeleted_DisplayOrder already created
            // by OptimizeObservationPhotoIndexes migration - removed duplicate

            migrationBuilder.AddForeignKey(
                name: "FK_ExercisePhotos_Organizations_OrganizationId",
                table: "ExercisePhotos",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExercisePhotos_Organizations_OrganizationId",
                table: "ExercisePhotos");

            // IX_ExercisePhotos_ObservationId_IsDeleted_DisplayOrder is owned by
            // OptimizeObservationPhotoIndexes migration - not dropped here

            migrationBuilder.DropIndex(
                name: "IX_ExercisePhotos_OrganizationId",
                table: "ExercisePhotos");
        }
    }
}
