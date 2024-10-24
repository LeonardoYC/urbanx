using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using urbanx.Data;
using Microsoft.EntityFrameworkCore;
using urbanx.Models;
using Microsoft.AspNetCore.Identity;
using urbanx.Service;

namespace urbanx.Controllers
{
    public class CatalogoController : Controller
    {
        private readonly ILogger<CatalogoController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly CartService _cartService;

        public CatalogoController(ILogger<CatalogoController> logger, ApplicationDbContext context, UserManager<IdentityUser> userManager, CartService cartService)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _cartService = cartService;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            var productos = _context.DataProducto.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                productos = productos.Where(s => s.Nombre.Contains(searchString) || s.Categoria.Contains(searchString));
            }

            productos = productos.Where(l => l.Estado == "Stock");

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await UpdateCartTotalItems(user.Email);
            }

            return View(await productos.ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producto = await _context.DataProducto.FindAsync(id);
            if (producto == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await UpdateCartTotalItems(user.Email);
            }

            return View(producto);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int id, string talla = "S", int cantidad = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                ViewData["Message"] = "Por favor debe loguearse antes de agregar un producto";
                return RedirectToAction(nameof(Index));
            }

            var producto = await _context.DataProducto.FindAsync(id);
            if (producto == null)
            {
                return NotFound();
            }

            var existingItem = await _context.DataItemCarrito
                .FirstOrDefaultAsync(c => c.UserID == user.Email && c.Producto.Id == id && c.Estado == "PENDIENTE" && c.Talla == talla);

            if (existingItem != null)
            {
                existingItem.Cantidad += cantidad;
                _context.Update(existingItem);
            }
            else
            {
                var carrito = new Carrito
                {
                    Producto = producto,
                    Precio = producto.Precio,
                    Talla = talla,
                    Cantidad = cantidad,
                    UserID = user.Email,
                    Estado = "PENDIENTE"
                };
                _context.Add(carrito);
            }

            await _context.SaveChangesAsync();
            await UpdateCartTotalItems(user.Email);

            ViewData["Message"] = "Se agregó al carrito";
            return RedirectToAction(nameof(Index));
        }

        private async Task UpdateCartTotalItems(string userEmail)
        {
            var totalItems = await _context.DataItemCarrito
                .Where(c => c.UserID == userEmail && c.Estado == "PENDIENTE")
                .SumAsync(c => c.Cantidad);
            _cartService.UpdateTotalItems(totalItems);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}