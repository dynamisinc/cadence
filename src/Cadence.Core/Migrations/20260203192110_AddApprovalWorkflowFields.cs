using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalWorkflowFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InjectApprovalPolicy",
                table: "Organizations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Injects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByUserId",
                table: "Injects",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApproverNotes",
                table: "Injects",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "Injects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectedByUserId",
                table: "Injects",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Injects",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedAt",
                table: "Injects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmittedByUserId",
                table: "Injects",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalOverriddenAt",
                table: "Exercises",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalOverriddenById",
                table: "Exercises",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalOverrideReason",
                table: "Exercises",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ApprovalPolicyOverridden",
                table: "Exercises",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireInjectApproval",
                table: "Exercises",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "InjectStatusHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ToStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_InjectStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InjectStatusHistories_AspNetUsers_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InjectStatusHistories_Injects_InjectId",
                        column: x => x.InjectId,
                        principalTable: "Injects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "InjectApprovalPolicy",
                value: "Optional");

            migrationBuilder.CreateIndex(
                name: "IX_Injects_ApprovedByUserId",
                table: "Injects",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Injects_RejectedByUserId",
                table: "Injects",
                column: "RejectedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Injects_SubmittedByUserId",
                table: "Injects",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Exercises_ApprovalOverriddenById",
                table: "Exercises",
                column: "ApprovalOverriddenById");

            migrationBuilder.CreateIndex(
                name: "IX_InjectStatusHistories_ChangedByUserId",
                table: "InjectStatusHistories",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InjectStatusHistories_InjectId",
                table: "InjectStatusHistories",
                column: "InjectId");

            migrationBuilder.CreateIndex(
                name: "IX_InjectStatusHistories_InjectId_ChangedAt",
                table: "InjectStatusHistories",
                columns: new[] { "InjectId", "ChangedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Exercises_AspNetUsers_ApprovalOverriddenById",
                table: "Exercises",
                column: "ApprovalOverriddenById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Injects_AspNetUsers_ApprovedByUserId",
                table: "Injects",
                column: "ApprovedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Injects_AspNetUsers_RejectedByUserId",
                table: "Injects",
                column: "RejectedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Injects_AspNetUsers_SubmittedByUserId",
                table: "Injects",
                column: "SubmittedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exercises_AspNetUsers_ApprovalOverriddenById",
                table: "Exercises");

            migrationBuilder.DropForeignKey(
                name: "FK_Injects_AspNetUsers_ApprovedByUserId",
                table: "Injects");

            migrationBuilder.DropForeignKey(
                name: "FK_Injects_AspNetUsers_RejectedByUserId",
                table: "Injects");

            migrationBuilder.DropForeignKey(
                name: "FK_Injects_AspNetUsers_SubmittedByUserId",
                table: "Injects");

            migrationBuilder.DropTable(
                name: "InjectStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_Injects_ApprovedByUserId",
                table: "Injects");

            migrationBuilder.DropIndex(
                name: "IX_Injects_RejectedByUserId",
                table: "Injects");

            migrationBuilder.DropIndex(
                name: "IX_Injects_SubmittedByUserId",
                table: "Injects");

            migrationBuilder.DropIndex(
                name: "IX_Exercises_ApprovalOverriddenById",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "InjectApprovalPolicy",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Injects");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "Injects");

            migrationBuilder.DropColumn(
                name: "ApproverNotes",
                table: "Injects");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "Injects");

            migrationBuilder.DropColumn(
                name: "RejectedByUserId",
                table: "Injects");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Injects");

            migrationBuilder.DropColumn(
                name: "SubmittedAt",
                table: "Injects");

            migrationBuilder.DropColumn(
                name: "SubmittedByUserId",
                table: "Injects");

            migrationBuilder.DropColumn(
                name: "ApprovalOverriddenAt",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "ApprovalOverriddenById",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "ApprovalOverrideReason",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "ApprovalPolicyOverridden",
                table: "Exercises");

            migrationBuilder.DropColumn(
                name: "RequireInjectApproval",
                table: "Exercises");
        }
    }
}
