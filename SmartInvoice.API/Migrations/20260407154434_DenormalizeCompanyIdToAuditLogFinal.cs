using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartInvoice.API.Migrations
{
    /// <inheritdoc />
    public partial class DenormalizeCompanyIdToAuditLogFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceAuditLogs_Companies_CompanyId",
                table: "InvoiceAuditLogs");

            // --- DATA MIGRATION ---
            // Populate CompanyId from Invoices for existing logs
            migrationBuilder.Sql(@"
                UPDATE ""InvoiceAuditLogs"" al
                SET ""CompanyId"" = i.""CompanyId""
                FROM ""Invoices"" i
                WHERE al.""InvoiceId"" = i.""InvoiceId"" AND al.""CompanyId"" IS NULL;
            ");

            // Clean up any logs that still have NULL CompanyId (unlikely but protection against FK violation)
            migrationBuilder.Sql(@"DELETE FROM ""InvoiceAuditLogs"" WHERE ""CompanyId"" IS NULL;");
            // ----------------------

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "InvoiceAuditLogs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceAuditLogs_Companies_CompanyId",
                table: "InvoiceAuditLogs",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "CompanyId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceAuditLogs_Companies_CompanyId",
                table: "InvoiceAuditLogs");

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "InvoiceAuditLogs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceAuditLogs_Companies_CompanyId",
                table: "InvoiceAuditLogs",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "CompanyId");
        }
    }
}
