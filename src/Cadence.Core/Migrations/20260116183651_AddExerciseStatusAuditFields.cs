using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseStatusAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActivatedAt",
                table: "Exercises",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ActivatedBy",
                table: "Exercises",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Exercises",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ArchivedBy",
                table: "Exercises",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Exercises",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompletedBy",
                table: "Exercises",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_ActivatedBy",
                table: "Exercises",
                column: "ActivatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_ArchivedBy",
                table: "Exercises",
                column: "ArchivedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_CompletedBy",
                table: "Exercises",
                column: "CompletedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_Users_ActivatedBy",
                table: "Exercises",
                column: "ActivatedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_Users_ArchivedBy",
                table: "Exercises",
                column: "ArchivedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_Users_CompletedBy",
                table: "Exercises",
                column: "CompletedBy",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_Users_ActivatedBy",
                table: "Exercises");

            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_Users_ArchivedBy",
                table: "Exercises");

            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_Users_CompletedBy",
                table: "Exercises");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_ActivatedBy",
                table: "Exercises");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_ArchivedBy",
                table: "Exercises");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_CompletedBy",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "ActivatedAt",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "ActivatedBy",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "ArchivedBy",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "CompletedBy",
                table: "Exercises");
        }
    }
}
