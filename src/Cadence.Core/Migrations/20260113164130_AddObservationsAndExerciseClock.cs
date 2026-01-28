using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddObservationsAndExerciseClock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "ClockElapsedBeforePause",
                table: "Exercises",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClockStartedAt",
                table: "Exercises",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClockStartedBy",
                table: "Exercises",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClockState",
                table: "Exercises",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Observations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Rating = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Recommendation = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ObservedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ExerciseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ObjectiveId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Observations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Observations_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Observations_Injects_InjectId",
                        column: x => x.InjectId,
                        principalTable: "Injects",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Observations_Objectives_ObjectiveId",
                        column: x => x.ObjectiveId,
                        principalTable: "Objectives",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Observations_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_ClockStartedBy",
                table: "Exercises",
                column: "ClockStartedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Observations_CreatedBy",
                table: "Observations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Observations_ExerciseId",
                table: "Observations",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_Observations_InjectId",
                table: "Observations",
                column: "InjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Observations_ObjectiveId",
                table: "Observations",
                column: "ObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_Observations_ObservedAt",
                table: "Observations",
                column: "ObservedAt");

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_Users_ClockStartedBy",
                table: "Exercises",
                column: "ClockStartedBy",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_Users_ClockStartedBy",
                table: "Exercises");

            migrationBuilder.DropTable(
                name: "Observations");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_ClockStartedBy",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "ClockElapsedBeforePause",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "ClockStartedAt",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "ClockStartedBy",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "ClockState",
                table: "Exercises");
        }
    }
}
