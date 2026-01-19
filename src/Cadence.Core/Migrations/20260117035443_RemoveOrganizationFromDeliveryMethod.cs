using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cadence.Core.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOrganizationFromDeliveryMethod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeliveryMethods_Organizations_OrganizationId",
                table: "DeliveryMethods");

            migrationBuilder.DropIndex(
                name: "IX_DeliveryMethods_OrganizationId_Name",
                table: "DeliveryMethods");

            migrationBuilder.DropIndex(
                name: "IX_DeliveryMethods_OrganizationId_SortOrder",
                table: "DeliveryMethods");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "DeliveryMethods");

            migrationBuilder.UpdateData(
                table: "DeliveryMethods",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                column: "SortOrder",
                value: 99);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryMethods_Name",
                table: "DeliveryMethods",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryMethods_SortOrder",
                table: "DeliveryMethods",
                column: "SortOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DeliveryMethods_Name",
                table: "DeliveryMethods");

            migrationBuilder.DropIndex(
                name: "IX_DeliveryMethods_SortOrder",
                table: "DeliveryMethods");

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "DeliveryMethods",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "DeliveryMethods",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "OrganizationId",
                value: null);

            migrationBuilder.UpdateData(
                table: "DeliveryMethods",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "OrganizationId",
                value: null);

            migrationBuilder.UpdateData(
                table: "DeliveryMethods",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                column: "OrganizationId",
                value: null);

            migrationBuilder.UpdateData(
                table: "DeliveryMethods",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                column: "OrganizationId",
                value: null);

            migrationBuilder.UpdateData(
                table: "DeliveryMethods",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                column: "OrganizationId",
                value: null);

            migrationBuilder.UpdateData(
                table: "DeliveryMethods",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                column: "OrganizationId",
                value: null);

            migrationBuilder.UpdateData(
                table: "DeliveryMethods",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                columns: new[] { "OrganizationId", "SortOrder" },
                values: new object[] { null, 7 });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryMethods_OrganizationId_Name",
                table: "DeliveryMethods",
                columns: new[] { "OrganizationId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryMethods_OrganizationId_SortOrder",
                table: "DeliveryMethods",
                columns: new[] { "OrganizationId", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_DeliveryMethods_Organizations_OrganizationId",
                table: "DeliveryMethods",
                column: "OrganizationId",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
