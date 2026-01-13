using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class RenameFireConditionAndParticipantBaseEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseParticipants_Users_AddedBy",
                table: "ExerciseParticipants");

            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseParticipants_Users_UserId",
                table: "ExerciseParticipants");

            migrationBuilder.DropIndex(
                name: "IX_ExerciseParticipants_AddedBy",
                table: "ExerciseParticipants");

            migrationBuilder.RenameColumn(
                name: "TriggerCondition",
                table: "Injects",
                newName: "FireCondition");

            migrationBuilder.RenameColumn(
                name: "AddedBy",
                table: "ExerciseParticipants",
                newName: "ModifiedBy");

            migrationBuilder.RenameColumn(
                name: "AddedAt",
                table: "ExerciseParticipants",
                newName: "UpdatedAt");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "ExerciseParticipants",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "ExerciseParticipants",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ExerciseParticipants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "ExerciseParticipants",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ExerciseParticipants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseParticipants_Users_UserId",
                table: "ExerciseParticipants",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseParticipants_Users_UserId",
                table: "ExerciseParticipants");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "ExerciseParticipants");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ExerciseParticipants");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ExerciseParticipants");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ExerciseParticipants");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ExerciseParticipants");

            migrationBuilder.RenameColumn(
                name: "FireCondition",
                table: "Injects",
                newName: "TriggerCondition");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "ExerciseParticipants",
                newName: "AddedAt");

            migrationBuilder.RenameColumn(
                name: "ModifiedBy",
                table: "ExerciseParticipants",
                newName: "AddedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseParticipants_AddedBy",
                table: "ExerciseParticipants",
                column: "AddedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseParticipants_Users_AddedBy",
                table: "ExerciseParticipants",
                column: "AddedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseParticipants_Users_UserId",
                table: "ExerciseParticipants",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
