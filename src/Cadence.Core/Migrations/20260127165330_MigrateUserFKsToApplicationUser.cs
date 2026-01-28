using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class MigrateUserFKsToApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_Users_ActivatedBy",
                table: "Exercises");

            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_Users_ArchivedBy",
                table: "Exercises");

            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_Users_ClockStartedBy",
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
                name: "IX_Exercises_ClockStartedBy",
                table: "Exercises");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_CompletedBy",
                table: "Exercises");

            migrationBuilder.AlterColumn<string>(
                name: "CompletedBy",
                table: "Exercises",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ClockStartedBy",
                table: "Exercises",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ArchivedBy",
                table: "Exercises",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ActivatedBy",
                table: "Exercises",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "CompletedBy",
                table: "Exercises",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ClockStartedBy",
                table: "Exercises",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ArchivedBy",
                table: "Exercises",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ActivatedBy",
                table: "Exercises",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_ActivatedBy",
                table: "Exercises",
                column: "ActivatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_ArchivedBy",
                table: "Exercises",
                column: "ArchivedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_ClockStartedBy",
                table: "Exercises",
                column: "ClockStartedBy");

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
                name: "FK_Exercises_Users_ClockStartedBy",
                table: "Exercises",
                column: "ClockStartedBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_Users_CompletedBy",
                table: "Exercises",
                column: "CompletedBy",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
