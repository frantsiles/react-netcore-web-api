using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FiscalCR.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantFiscalCrConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantConfigs",
                schema: "fiscal_cr",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RazonSocial = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NombreComercial = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TipoIdentificacion = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    NumeroIdentificacion = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    CodigoActividad = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    Provincia = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    Canton = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Distrito = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    OtrasSenas = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    HaciendaUsername = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    HaciendaPassword = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CertificateBytes = table.Column<byte[]>(type: "bytea", nullable: true),
                    CertificatePassword = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Environment = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantConfigs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantConfigs_TenantId",
                schema: "fiscal_cr",
                table: "TenantConfigs",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantConfigs",
                schema: "fiscal_cr");
        }
    }
}
