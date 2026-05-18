using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscalMX.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "fiscal_mx");

            migrationBuilder.CreateTable(
                name: "cfdi_documents",
                schema: "fiscal_mx",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Uuid = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    Serie = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: true),
                    Folio = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    XmlOriginal = table.Column<string>(type: "text", nullable: true),
                    XmlTimbrado = table.Column<string>(type: "text", nullable: true),
                    PacMensaje = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CancelMotivo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TimbradoAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cfdi_documents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cfdi_documents_InvoiceId",
                schema: "fiscal_mx",
                table: "cfdi_documents",
                column: "InvoiceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cfdi_documents_TenantId_Status",
                schema: "fiscal_mx",
                table: "cfdi_documents",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_cfdi_documents_Uuid",
                schema: "fiscal_mx",
                table: "cfdi_documents",
                column: "Uuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cfdi_documents",
                schema: "fiscal_mx");
        }
    }
}
