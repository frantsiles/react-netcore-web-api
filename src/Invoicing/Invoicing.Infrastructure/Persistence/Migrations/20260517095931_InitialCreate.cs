using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Invoicing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "invoicing");

            migrationBuilder.CreateTable(
                name: "Invoices",
                schema: "invoicing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginSalesOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Subtotal_Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Subtotal_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TaxAmount_Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    TaxAmount_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TotalAmount_Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalAmount_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PaidAmount_Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    PaidAmount_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    BalanceDue_Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    BalanceDue_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InvoiceLine",
                schema: "invoicing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginSalesOrderLineId = table.Column<Guid>(type: "uuid", nullable: true),
                    CatalogItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    SKU = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitPrice_Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitPrice_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "numeric", nullable: false),
                    LineTotal_Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    LineTotal_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceLine", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InvoiceLine_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "invoicing",
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceLine_InvoiceId",
                schema: "invoicing",
                table: "InvoiceLine",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId_InvoiceNumber",
                schema: "invoicing",
                table: "Invoices",
                columns: new[] { "TenantId", "InvoiceNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceLine",
                schema: "invoicing");

            migrationBuilder.DropTable(
                name: "Invoices",
                schema: "invoicing");
        }
    }
}
