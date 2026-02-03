using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalNotificationsAndRevertFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RevertReason",
                table: "Injects",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RevertedAt",
                table: "Injects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevertedByUserId",
                table: "Injects",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ApprovalNotifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ExerciseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TriggeredByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalNotifications_AspNetUsers_TriggeredByUserId",
                        column: x => x.TriggeredByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApprovalNotifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovalNotifications_Exercises_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "Exercises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovalNotifications_Injects_InjectId",
                        column: x => x.InjectId,
                        principalTable: "Injects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApprovalNotifications_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Injects_RevertedByUserId",
                table: "Injects",
                column: "RevertedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalNotifications_ExerciseId",
                table: "ApprovalNotifications",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalNotifications_InjectId",
                table: "ApprovalNotifications",
                column: "InjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalNotifications_OrganizationId",
                table: "ApprovalNotifications",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalNotifications_TriggeredByUserId",
                table: "ApprovalNotifications",
                column: "TriggeredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalNotifications_UserId_CreatedAt",
                table: "ApprovalNotifications",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalNotifications_UserId_IsRead",
                table: "ApprovalNotifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.AddForeignKey(
                name: "FK_Injects_AspNetUsers_RevertedByUserId",
                table: "Injects",
                column: "RevertedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Injects_AspNetUsers_RevertedByUserId",
                table: "Injects");

            migrationBuilder.DropTable(
                name: "ApprovalNotifications");

            migrationBuilder.DropIndex(
                name: "IX_Injects_RevertedByUserId",
                table: "Injects");

            migrationBuilder.DropColumn(
                name: "RevertReason",
                table: "Injects");

            migrationBuilder.DropColumn(
                name: "RevertedAt",
                table: "Injects");

            migrationBuilder.DropColumn(
                name: "RevertedByUserId",
                table: "Injects");
        }
    }
}
