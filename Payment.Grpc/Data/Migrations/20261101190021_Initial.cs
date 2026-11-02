using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Payment.Grpc.Migrations
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
                    ReservaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Aprobado = table.Column<bool>(type: "INTEGER", nullable: false),
                    Monto = table.Column<int>(type: "INTEGER", precision: 18, scale: 2, nullable: false),
                    FechaTransaccion = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_Pagos_SolicitudId",
                table: "Pagos",
                column: "SolicitudId",
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
