using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace urbanx.Models
{
    [Table("t_producto")]
    public class Producto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [NotNull]
        public string? Nombre { get; set; }
        [NotNull]
        public string? Descripcion { get; set; }
        [NotNull]
        public Decimal Precio { get; set; }
        [NotNull]
        public string? Categoria { get; set; }
        [NotNull]
        public int? Cantidad { get; set; }
        [NotNull]
        public string? Estado { get; set; }
        public string? ImageURL { get; set; }
    }
}