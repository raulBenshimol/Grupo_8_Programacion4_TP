using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payment.Grpc.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pagos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SolicitudId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReservaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Aprobado = table.Column<bool>(type: "INTEGER", nullable: false),
                    MontoCentavos = table.Column<long>(type: "INTEGER", nullable: false),
                    CodigoTransferencia = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    FechaTransaccion = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_Pagos_ReservaId",
                table: "Pagos",
                column: "ReservaId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Pagos");
        }
    }
}
