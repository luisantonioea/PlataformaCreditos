using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PlataformaCreditos.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgresV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "SolicitudesCredito",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "SolicitudesCredito",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Clientes",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Clientes",
                keyColumn: "Id",
                keyValue: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Clientes",
                columns: new[] { "Id", "Activo", "IngresosMensuales", "UsuarioId" },
                values: new object[,]
                {
                    { 1, true, 2000m, "user-mock-1" },
                    { 2, true, 3500m, "user-mock-2" }
                });

            migrationBuilder.InsertData(
                table: "SolicitudesCredito",
                columns: new[] { "Id", "ClienteId", "Estado", "FechaSolicitud", "MontoSolicitado", "MotivoRechazo" },
                values: new object[,]
                {
                    { 1, 1, 1, new DateTime(2026, 4, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), 1500m, null },
                    { 2, 2, 0, new DateTime(2026, 4, 25, 0, 0, 0, 0, DateTimeKind.Unspecified), 5000m, null }
                });
        }
    }
}
