using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class MigrateInjectUserFKsToApplicationUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Injects_Users_FiredBy",
                table: "Injects");

            migrationBuilder.DropForeignKey(
                name: "FK_Injects_Users_SkippedBy",
                table: "Injects");

            migrationBuilder.DropIndex(
                name: "IX_Injects_FiredBy",
                table: "Injects");

            migrationBuilder.DropIndex(
                name: "IX_Injects_SkippedBy",
                table: "Injects");

            migrationBuilder.DropColumn(
                name: "FiredBy",
                table: "Injects");

            migrationBuilder.DropColumn(
                name: "SkippedBy",
                table: "Injects");

            migrationBuilder.AddColumn<string>(
                name: "FiredByUserId",
                table: "Injects",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SkippedByUserId",
                table: "Injects",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Injects_FiredByUserId",
                table: "Injects",
                column: "FiredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Injects_SkippedByUserId",
                table: "Injects",
                column: "SkippedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Injects_AspNetUsers_FiredByUserId",
                table: "Injects",
                column: "FiredByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Injects_AspNetUsers_SkippedByUserId",
                table: "Injects",
                column: "SkippedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Injects_AspNetUsers_FiredByUserId",
                table: "Injects");

            migrationBuilder.DropForeignKey(
                name: "FK_Injects_AspNetUsers_SkippedByUserId",
                table: "Injects");

            migrationBuilder.DropIndex(
                name: "IX_Injects_FiredByUserId",
                table: "Injects");

            migrationBuilder.DropIndex(
                name: "IX_Injects_SkippedByUserId",
                table: "Injects");

            migrationBuilder.DropColumn(
                name: "FiredByUserId",
                table: "Injects");

            migrationBuilder.DropColumn(
                name: "SkippedByUserId",
                table: "Injects");

            migrationBuilder.AddColumn<Guid>(
                name: "FiredBy",
                table: "Injects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SkippedBy",
                table: "Injects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Injects_FiredBy",
                table: "Injects",
                column: "FiredBy");

            migrationBuilder.CreateIndex(
                name: "IX_Injects_SkippedBy",
                table: "Injects",
                column: "SkippedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Injects_Users_FiredBy",
                table: "Injects",
                column: "FiredBy",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Injects_Users_SkippedBy",
                table: "Injects",
                column: "SkippedBy",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
