using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscalCR.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "fiscal_cr");

            migrationBuilder.CreateTable(
                name: "ElectronicDocuments",
                schema: "fiscal_cr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Clave = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NumeroConsecutivo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    XmlUnsigned = table.Column<string>(type: "text", nullable: true),
                    XmlSigned = table.Column<string>(type: "text", nullable: true),
                    HaciendaMensaje = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    HaciendaXmlRespuesta = table.Column<string>(type: "text", nullable: true),
                    HaciendaEstado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AcknowledgedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectronicDocuments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicDocuments_Clave",
                schema: "fiscal_cr",
                table: "ElectronicDocuments",
                column: "Clave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicDocuments_InvoiceId",
                schema: "fiscal_cr",
                table: "ElectronicDocuments",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicDocuments_TenantId_Status",
                schema: "fiscal_cr",
                table: "ElectronicDocuments",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ElectronicDocuments",
                schema: "fiscal_cr");
        }
    }
}
