using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using urbanx.Data;
using urbanx.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Dynamic;

namespace urbanx.Controllers
{
    public class CarritoController : Controller
    {
        private readonly ILogger<CarritoController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly CartService _cartService;

        public CarritoController(ILogger<CarritoController> logger, ApplicationDbContext context, UserManager<IdentityUser> userManager, CartService cartService)
        {
            _logger = logger;
            _userManager = userManager;
            _context = context;
            _cartService = cartService;
        }

        public IActionResult IndexUltimoProductoSesion()
        {
            var producto = Helper.SessionExtensions.Get<Producto>(HttpContext.Session, "MiUltimoProducto");
            return View("UltimoProducto", producto);
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                ViewData["Message"] = "Por favor debe loguearse antes de ver el carrito";
                return RedirectToAction("Index", "Catalogo");
            }

            var items = await _context.DataItemCarrito
                .Include(p => p.Producto)
                .Where(w => w.UserID == user.Email && w.Estado == "PENDIENTE")
                .GroupBy(i => new { i.Producto.Id, i.Producto.Nombre, i.Talla })
                .Select(g => new Carrito
                {
                    Producto = g.First().Producto,
                    Talla = g.Key.Talla,
                    Cantidad = g.Sum(i => i.Cantidad),
                    Precio = g.First().Precio,
                    UserID = user.Email,
                    Estado = "PENDIENTE"
                })
                .ToListAsync();

            var total = items.Sum(i => i.Cantidad * i.Precio);

            ViewData["MontoTotal"] = total;
            await UpdateCartTotalItems(user.Email);

            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Comprar(int id, string Talla, int Cantidad)
        {
            var userID = _userManager.GetUserName(User);
            if (userID == null)
            {
                _logger.LogInformation("No existe usuario");
                ViewData["Message"] = "Por favor debe loguearse antes de comprar un producto";
                return RedirectToAction("Index", "Catalogo");
            }

            var producto = await _context.DataProducto.FindAsync(id);

            Carrito carrito = new Carrito
            {
                Producto = producto,
                Precio = producto.Precio,
                Cantidad = Cantidad,
                Talla = Talla,
                UserID = userID
            };

            _context.Add(carrito);
            await _context.SaveChangesAsync();

            await UpdateCartTotalItems(userID);

            ViewData["Message"] = "Se ha añadido el producto al carrito y se le redirigirá a la vista del carrito.";
            _logger.LogInformation("Se ha comprado un producto y se ha añadido al carrito.");

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Add(int? id, string Talla, int Cantidad)
        {
            var userID = _userManager.GetUserName(User);
            if (userID == null)
            {
                _logger.LogInformation("No existe usuario");
                ViewData["Message"] = "Por favor debe loguearse antes de agregar un producto";
                return RedirectToAction("Index", "Catalogo");
            }
            else
            {
                var producto = await _context.DataProducto.FindAsync(id);
                Helper.SessionExtensions.Set<Producto>(HttpContext.Session, "MiUltimoProducto", producto);

                Carrito carrito = new Carrito
                {
                    Producto = producto,
                    Precio = producto.Precio,
                    Cantidad = Cantidad,
                    Talla = Talla,
                    UserID = userID
                };

                _context.Add(carrito);
                await _context.SaveChangesAsync();

                await UpdateCartTotalItems(userID);

                ViewData["Message"] = "Se Agrego al carrito";
                _logger.LogInformation("Se agrego un producto al carrito");
                return RedirectToAction("Index", "Catalogo");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int? id, string talla)
        {
            if (id == null || string.IsNullOrEmpty(talla))
            {
                return NotFound();
            }

            var itemsCarrito = await _context.DataItemCarrito
                .Where(c => c.Producto.Id == id && c.Talla == talla)
                .ToListAsync();

            if (!itemsCarrito.Any())
            {
                return NotFound();
            }

            _context.DataItemCarrito.RemoveRange(itemsCarrito);
            await _context.SaveChangesAsync();

            await UpdateCartTotalItems(itemsCarrito.First().UserID);

            return RedirectToAction(nameof(Index));
        }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, string talla)
    {
        if (id == 0)
        {
            return NotFound();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToAction("Index", "Home");
        }

        var itemCarrito = await _context.DataItemCarrito
            .Include(c => c.Producto)
            .FirstOrDefaultAsync(c => c.Producto.Id == id && c.Talla == talla && c.UserID == user.Email);

        if (itemCarrito == null)
        {
            return NotFound();
        }

        return View(itemCarrito);
    }

    // Método POST para procesar la edición
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCarrito(int id, [Bind("Cantidad,Talla,UserID,Precio,Estado")] Carrito itemCarrito)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null || user.Email != itemCarrito.UserID)
        {
            return RedirectToAction("Index", "Home");
        }

        try
        {
            // Buscar los items existentes
            var existingItems = await _context.DataItemCarrito
                .Include(c => c.Producto)
                .Where(c => c.Producto.Id == id && c.UserID == user.Email)
                .ToListAsync();

            if (!existingItems.Any())
            {
                return NotFound();
            }

            // Eliminar los items existentes
            _context.DataItemCarrito.RemoveRange(existingItems);
            await _context.SaveChangesAsync();

            // Crear nuevo item con la talla actualizada
            var producto = await _context.DataProducto.FindAsync(id);
            if (producto == null)
            {
                return NotFound();
            }

            var nuevoItem = new Carrito
            {
                Producto = producto,
                Cantidad = itemCarrito.Cantidad,
                Talla = itemCarrito.Talla,
                Precio = itemCarrito.Precio,
                UserID = user.Email,
                Estado = "PENDIENTE"
            };

            _context.Add(nuevoItem);
            await _context.SaveChangesAsync();
            await UpdateCartTotalItems(user.Email);

            TempData["Message"] = "Producto actualizado exitosamente";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Error"] = "Error al actualizar el producto";
            return RedirectToAction(nameof(Index));
        }
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
            return View("Error!");
        }
    }
}