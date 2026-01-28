using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddTimingConfigurationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeliveryMode",
                table: "Exercises",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "ClockDriven");

            migrationBuilder.AddColumn<decimal>(
                name: "TimeScale",
                table: "Exercises",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimelineMode",
                table: "Exercises",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "RealTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryMode",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "TimeScale",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "TimelineMode",
                table: "Exercises");
        }
    }
}
