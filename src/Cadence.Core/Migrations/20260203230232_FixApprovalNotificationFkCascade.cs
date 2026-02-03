using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class FixApprovalNotificationFkCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalNotifications_Injects_InjectId",
                table: "ApprovalNotifications");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalNotifications_Injects_InjectId",
                table: "ApprovalNotifications",
                column: "InjectId",
                principalTable: "Injects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApprovalNotifications_Injects_InjectId",
                table: "ApprovalNotifications");

            migrationBuilder.AddForeignKey(
                name: "FK_ApprovalNotifications_Injects_InjectId",
                table: "ApprovalNotifications",
                column: "InjectId",
                principalTable: "Injects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
