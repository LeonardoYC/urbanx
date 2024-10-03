using System.ComponentModel.DataAnnotations.Schema;

namespace urbanx.Models
{
    [Table("t_pedido")]
    public class Pedido
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int ID {get; set;}

        public string? UserID{ get; set; }

        public Decimal Total { get; set; }

        public Pago? pago { get; set; }

        public string? Status { get; set; }
    }
}