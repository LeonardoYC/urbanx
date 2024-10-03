using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace urbanx.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<urbanx.Models.Contacto> DataContacto {get; set; }
    public DbSet<urbanx.Models.Producto> DataProducto {get; set; }
    public DbSet<urbanx.Models.Carrito> DataItemCarrito {get; set; }
    public DbSet<urbanx.Models.Pago> Pago {get; set; }
    public DbSet<urbanx.Models.Pedido> Pedido  {get; set; }
    public DbSet<urbanx.Models.DetallePedido> DetallePedido  {get; set; }

    
}