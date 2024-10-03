using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace urbanx.Migrations
{
    /// <inheritdoc />
    public partial class MigracionInicial2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "t_detalle_pedido",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductoId = table.Column<int>(type: "integer", nullable: true),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric", nullable: false),
                    pedidoID = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_detalle_pedido", x => x.id);
                    table.ForeignKey(
                        name: "FK_t_detalle_pedido_t_pedido_pedidoID",
                        column: x => x.pedidoID,
                        principalTable: "t_pedido",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_t_detalle_pedido_t_producto_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "t_producto",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_t_detalle_pedido_pedidoID",
                table: "t_detalle_pedido",
                column: "pedidoID");

            migrationBuilder.CreateIndex(
                name: "IX_t_detalle_pedido_ProductoId",
                table: "t_detalle_pedido",
                column: "ProductoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "t_detalle_pedido");
        }
    }
}
