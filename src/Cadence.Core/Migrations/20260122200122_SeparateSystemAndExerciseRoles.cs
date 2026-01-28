using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class SeparateSystemAndExerciseRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseParticipants_Users_UserId",
                table: "ExerciseParticipants");

            migrationBuilder.DropIndex(
                name: "IX_ExerciseParticipants_ExerciseId_UserId",
                table: "ExerciseParticipants");

            migrationBuilder.DropColumn(
                name: "GlobalRole",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Exercises",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "ExerciseParticipants",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAt",
                table: "ExerciseParticipants",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "AssignedById",
                table: "ExerciseParticipants",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "ExerciseParticipants",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SystemRole",
                table: "AspNetUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_ApplicationUserId",
                table: "Exercises",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseParticipants_AssignedById",
                table: "ExerciseParticipants",
                column: "AssignedById");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseParticipants_ExerciseId_UserId",
                table: "ExerciseParticipants",
                columns: new[] { "ExerciseId", "UserId" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseParticipants_UserId1",
                table: "ExerciseParticipants",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_SystemRole",
                table: "AspNetUsers",
                column: "SystemRole");

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseParticipants_AspNetUsers_AssignedById",
                table: "ExerciseParticipants",
                column: "AssignedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseParticipants_AspNetUsers_UserId",
                table: "ExerciseParticipants",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseParticipants_Users_UserId1",
                table: "ExerciseParticipants",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_AspNetUsers_ApplicationUserId",
                table: "Exercises",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseParticipants_AspNetUsers_AssignedById",
                table: "ExerciseParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseParticipants_AspNetUsers_UserId",
                table: "ExerciseParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseParticipants_Users_UserId1",
                table: "ExerciseParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_AspNetUsers_ApplicationUserId",
                table: "Exercises");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_ApplicationUserId",
                table: "Exercises");

            migrationBuilder.DropIndex(
                name: "IX_ExerciseParticipants_AssignedById",
                table: "ExerciseParticipants");

            migrationBuilder.DropIndex(
                name: "IX_ExerciseParticipants_ExerciseId_UserId",
                table: "ExerciseParticipants");

            migrationBuilder.DropIndex(
                name: "IX_ExerciseParticipants_UserId1",
                table: "ExerciseParticipants");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_SystemRole",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "ExerciseParticipants");

            migrationBuilder.DropColumn(
                name: "AssignedById",
                table: "ExerciseParticipants");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "ExerciseParticipants");

            migrationBuilder.DropColumn(
                name: "SystemRole",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "ExerciseParticipants",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GlobalRole",
                table: "AspNetUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseParticipants_ExerciseId_UserId",
                table: "ExerciseParticipants",
                columns: new[] { "ExerciseId", "UserId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseParticipants_Users_UserId",
                table: "ExerciseParticipants",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
