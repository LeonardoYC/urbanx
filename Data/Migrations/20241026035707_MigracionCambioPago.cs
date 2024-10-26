using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace urbanx.Migrations
{
    /// <inheritdoc />
    public partial class MigracionCambioPago : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NombreTarjeta",
                table: "t_pago");

            migrationBuilder.DropColumn(
                name: "NumeroTarjeta",
                table: "t_pago");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NombreTarjeta",
                table: "t_pago",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroTarjeta",
                table: "t_pago",
                type: "text",
                nullable: true);
        }
    }
}
