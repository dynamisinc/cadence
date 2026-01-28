using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoFireEnabled",
                table: "Exercises",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ClockMultiplier",
                table: "Exercises",
                type: "decimal(4,2)",
                nullable: false,
                defaultValue: 1.0m);

            migrationBuilder.AddColumn<bool>(
                name: "ConfirmClockControl",
                table: "Exercises",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ConfirmFireInject",
                table: "Exercises",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ConfirmSkipInject",
                table: "Exercises",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoFireEnabled",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "ClockMultiplier",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "ConfirmClockControl",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "ConfirmFireInject",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "ConfirmSkipInject",
                table: "Exercises");
        }
    }
}
