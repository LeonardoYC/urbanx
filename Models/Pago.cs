using System.ComponentModel.DataAnnotations.Schema;

namespace urbanx.Models
{
    [Table("t_pago")]
    public class Pago
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }
        public DateTime PaymentDate { get; set; }
        public Decimal MontoTotal
        { get; set; }
        public string? UserID { get; set; }
    }
}
