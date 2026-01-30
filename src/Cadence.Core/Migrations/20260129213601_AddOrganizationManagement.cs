using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Organizations",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Organizations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Organizations",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentOrganizationId",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Agencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Abbreviation = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Agencies_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationInvites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    MaxUses = table.Column<int>(type: "int", nullable: false),
                    UseCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationInvites_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrganizationInvites_AspNetUsers_UsedById",
                        column: x => x.UsedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrganizationInvites_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InvitedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModifiedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationMemberships_AspNetUsers_InvitedById",
                        column: x => x.InvitedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrganizationMemberships_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrganizationMemberships_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Organizations",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                columns: new[] { "ContactEmail", "Slug", "Status" },
                values: new object[] { null, "default", "Active" });

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Slug",
                table: "Organizations",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Status",
                table: "Organizations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CurrentOrganizationId",
                table: "AspNetUsers",
                column: "CurrentOrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Agencies_OrganizationId_IsActive_SortOrder",
                table: "Agencies",
                columns: new[] { "OrganizationId", "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Agencies_OrganizationId_Name",
                table: "Agencies",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationInvites_Code",
                table: "OrganizationInvites",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationInvites_CreatedByUserId",
                table: "OrganizationInvites",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationInvites_Email",
                table: "OrganizationInvites",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationInvites_OrganizationId",
                table: "OrganizationInvites",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationInvites_OrganizationId_ExpiresAt",
                table: "OrganizationInvites",
                columns: new[] { "OrganizationId", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationInvites_UsedById",
                table: "OrganizationInvites",
                column: "UsedById");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_InvitedById",
                table: "OrganizationMemberships",
                column: "InvitedById");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_OrganizationId",
                table: "OrganizationMemberships",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_OrganizationId_Status",
                table: "OrganizationMemberships",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationMemberships_UserId_OrganizationId",
                table: "OrganizationMemberships",
                columns: new[] { "UserId", "OrganizationId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Organizations_CurrentOrganizationId",
                table: "AspNetUsers",
                column: "CurrentOrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Organizations_CurrentOrganizationId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Agencies");

            migrationBuilder.DropTable(
                name: "OrganizationInvites");

            migrationBuilder.DropTable(
                name: "OrganizationMemberships");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_Slug",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_Status",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CurrentOrganizationId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "CurrentOrganizationId",
                table: "AspNetUsers");
        }
    }
}
