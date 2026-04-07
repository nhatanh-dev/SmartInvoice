using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartInvoice.API.Migrations
{
    /// <inheritdoc />
    public partial class DenormalizeCompanyIdToAuditLogNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "InvoiceAuditLogs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceAuditLogs_CompanyId",
                table: "InvoiceAuditLogs",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceAuditLogs_Companies_CompanyId",
                table: "InvoiceAuditLogs",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "CompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceAuditLogs_Companies_CompanyId",
                table: "InvoiceAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceAuditLogs_CompanyId",
                table: "InvoiceAuditLogs");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "InvoiceAuditLogs");
        }
    }
}
