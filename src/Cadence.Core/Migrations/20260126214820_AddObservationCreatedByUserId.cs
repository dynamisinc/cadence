using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddObservationCreatedByUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Observations_Users_CreatedBy",
                table: "Observations");

            migrationBuilder.DropIndex(
                name: "IX_Observations_CreatedBy",
                table: "Observations");

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Observations",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Observations_CreatedByUserId",
                table: "Observations",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Observations_AspNetUsers_CreatedByUserId",
                table: "Observations",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Observations_AspNetUsers_CreatedByUserId",
                table: "Observations");

            migrationBuilder.DropIndex(
                name: "IX_Observations_CreatedByUserId",
                table: "Observations");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Observations");

            migrationBuilder.CreateIndex(
                name: "IX_Observations_CreatedBy",
                table: "Observations",
                column: "CreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Observations_Users_CreatedBy",
                table: "Observations",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
