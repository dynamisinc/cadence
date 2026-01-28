using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddHseepRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HseepRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsSystemWide = table.Column<bool>(type: "bit", nullable: false),
                    CanFireInjects = table.Column<bool>(type: "bit", nullable: false),
                    CanRecordObservations = table.Column<bool>(type: "bit", nullable: false),
                    CanManageExercise = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HseepRoles", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "HseepRoles",
                columns: new[] { "Id", "CanFireInjects", "CanManageExercise", "CanRecordObservations", "Code", "Description", "IsActive", "IsSystemWide", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 1, true, true, true, "Administrator", "System-wide configuration and user management. Has full access to all exercises within their organization.", true, true, "Administrator", 1 },
                    { 2, true, true, true, "ExerciseDirector", "Full exercise management authority. Responsible for Go/No-Go decisions and overall exercise conduct.", true, false, "Exercise Director", 2 },
                    { 3, true, false, false, "Controller", "Delivers injects to players and manages scenario flow during exercise conduct.", true, false, "Controller", 3 },
                    { 4, false, false, true, "Evaluator", "Observes and documents player performance for the After-Action Report (AAR).", true, false, "Evaluator", 4 },
                    { 5, false, false, false, "Observer", "Watches exercise conduct without interfering. Read-only access to exercise data.", true, false, "Observer", 5 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_HseepRoles_Code",
                table: "HseepRoles",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HseepRoles");
        }
    }
}
