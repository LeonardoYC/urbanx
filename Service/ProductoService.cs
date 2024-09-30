using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using urbanx.Data;
using urbanx.Models;
using UrbanX.Models;

namespace urbanx.Service
{
    public class ProductoService
    {
        private readonly ILogger<ProductoService> _logger;
        private readonly ApplicationDbContext _context;

        public ProductoService(ILogger<ProductoService> logger,ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<List<Producto>?> GetAll(){
            if(_context.DataProducto == null )
                return null;
            return await _context.DataProducto.ToListAsync();
        }

        public async Task<Producto?> GetProductoById(int id)
        {
            if (_context.DataProducto == null)
                return null;

            var producto = await _context.DataProducto
                .FirstOrDefaultAsync(p => p.Id == id);

            return producto;
        }
    }
}