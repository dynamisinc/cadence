using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddEulaSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EulaContent",
                table: "SystemSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EulaUpdatedAt",
                table: "SystemSettings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EulaVersion",
                table: "SystemSettings",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EulaAcceptances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    EulaVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EulaAcceptances", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EulaAcceptances_UserId_EulaVersion",
                table: "EulaAcceptances",
                columns: new[] { "UserId", "EulaVersion" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EulaAcceptances");

            migrationBuilder.DropColumn(
                name: "EulaContent",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "EulaUpdatedAt",
                table: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "EulaVersion",
                table: "SystemSettings");
        }
    }
}
