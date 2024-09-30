using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace urbanx.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<UrbanX.Models.Contacto> DataContacto { get; set; }
    public DbSet<UrbanX.Models.Producto> DataProducto { get; set; }
    
    }
