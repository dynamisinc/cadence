using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddCapabilityCoveringIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Capabilities_OrganizationId_IsActive_Category_SortOrder_Name",
                table: "Capabilities",
                columns: new[] { "OrganizationId", "IsActive", "Category", "SortOrder", "Name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Capabilities_OrganizationId_IsActive_Category_SortOrder_Name",
                table: "Capabilities");
        }
    }
}
