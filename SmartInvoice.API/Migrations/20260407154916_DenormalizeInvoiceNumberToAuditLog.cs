using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartInvoice.API.Migrations
{
    /// <inheritdoc />
    public partial class DenormalizeInvoiceNumberToAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                table: "InvoiceAuditLogs",
                type: "text",
                nullable: true);

            // --- DATA MIGRATION: Populate InvoiceNumber from Invoices for existing logs ---
            migrationBuilder.Sql(@"
                UPDATE ""InvoiceAuditLogs"" al
                SET ""InvoiceNumber"" = i.""InvoiceNumber""
                FROM ""Invoices"" i
                WHERE al.""InvoiceId"" = i.""InvoiceId"" AND al.""InvoiceNumber"" IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                table: "InvoiceAuditLogs");
        }
    }
}
