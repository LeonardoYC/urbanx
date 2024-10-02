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

        public CarritoController(ILogger<CarritoController> logger, ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _userManager = userManager;
            _context = context;
        }

        public IActionResult IndexUltimoProductoSesion()
        {
            var producto  = Helper.SessionExtensions.Get<Producto>(HttpContext.Session,"MiUltimoProducto");
            return View("UltimoProducto",producto);
        }

        public IActionResult Index()
        {
            var userIDSession = _userManager.GetUserName(User);
            if(userIDSession == null){
                ViewData["Message"] = "Por favor debe loguearse antes de agregar un producto";
                return RedirectToAction("Index","Catalogo");
            }
            var items = from o in _context.DataItemCarrito select o;
            items = items.Include(p => p.Producto).
                    Where(w => w.UserID.Equals(userIDSession) &&
                        w.Estado.Equals("PENDIENTE"));
            var itemsCarrito = items.ToList();
            var total = itemsCarrito.Sum(c => c.Cantidad * c.Precio);

            dynamic model = new ExpandoObject();
            model.montoTotal = total;
            model.elementosCarrito = itemsCarrito;
            return View(model);
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

            // Crear el objeto Carrito y asignar la talla seleccionada
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

            ViewData["Message"] = "Se ha añadido el producto al carrito y se le redirigirá a la vista del carrito.";
            _logger.LogInformation("Se ha comprado un producto y se ha añadido al carrito.");

            // Redirigir a la vista del carrito
            return RedirectToAction("Index"); // Asegúrate de que esto redirija a la acción correcta
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

                // Crear el objeto Carrito y asignar la talla seleccionada
                Carrito carrito = new Carrito();
                carrito.Producto = producto;
                carrito.Precio = producto.Precio;
                carrito.Cantidad = Cantidad;
                carrito.Talla = Talla; // Aquí usas la talla seleccionada por el usuario
                carrito.UserID = userID;

                _context.Add(carrito);
                await _context.SaveChangesAsync();

                ViewData["Message"] = "Se Agrego al carrito";
                _logger.LogInformation("Se agrego un producto al carrito");
                return RedirectToAction("Index", "Catalogo");
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var itemCarrito = await _context.DataItemCarrito.FindAsync(id);
            _context.DataItemCarrito.Remove(itemCarrito);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var itemCarrito = await _context.DataItemCarrito.FindAsync(id);
            if (itemCarrito == null)
            {
                return NotFound();
            }
            return View(itemCarrito);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,UserID,Precio,Talla,Cantidad")] Carrito itemCarrito)
        {
            if (id != itemCarrito.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(itemCarrito);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.DataItemCarrito.Any(e => e.Id == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(itemCarrito);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}