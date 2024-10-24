using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using urbanx.ViewModels;

namespace urbanx.Models
{
    [Table("t_carrito")]
    public class Carrito
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string? UserID { get; set; }
        public Producto? Producto { get; set; }
        public Decimal Precio { get; set; }
        public string? Talla { get; set; }
        [NotNull]
        public int Cantidad { get; set; }
        [NotNull]
        public string Estado { get; set; } = "PENDIENTE";
    }
}