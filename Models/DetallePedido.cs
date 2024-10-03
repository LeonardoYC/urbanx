using System.ComponentModel.DataAnnotations.Schema;

namespace urbanx.Models
{
    [Table("t_detalle_pedido")]
    public class DetallePedido
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int ID {get; set;}

        public urbanx.Models.Producto? Producto {get; set;}

        public int Cantidad{get; set;}
        public Decimal Precio { get; set; }
        public Pedido? pedido {get; set;}
    }
}