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

            migrationBuilder.CreateIndex(
                name: "IX_ExercisePhotos_ObservationId_IsDeleted_DisplayOrder",
                table: "ExercisePhotos",
                columns: new[] { "ObservationId", "IsDeleted", "DisplayOrder" });

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

            migrationBuilder.DropIndex(
                name: "IX_ExercisePhotos_ObservationId_IsDeleted_DisplayOrder",
                table: "ExercisePhotos");

            migrationBuilder.DropIndex(
                name: "IX_ExercisePhotos_OrganizationId",
                table: "ExercisePhotos");
        }
    }
}
