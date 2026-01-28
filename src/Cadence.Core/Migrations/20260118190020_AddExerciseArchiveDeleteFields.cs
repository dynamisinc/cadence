using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseArchiveDeleteFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasBeenPublished",
                table: "Exercises",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PreviousStatus",
                table: "Exercises",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            // Set HasBeenPublished = true for any exercise that's not in Draft status
            // Draft = 'Draft', all other statuses indicate the exercise has been published at some point
            migrationBuilder.Sql(@"
                UPDATE Exercises
                SET HasBeenPublished = 1
                WHERE Status != 'Draft'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasBeenPublished",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "PreviousStatus",
                table: "Exercises");
        }
    }
}
