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

namespace urbanx.Controllers
{
    public class CatalogoController : Controller
    {
        private readonly ILogger<CatalogoController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CatalogoController(ILogger<CatalogoController> logger, ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index(string? searchString)
        {
            var productos = from o in _context.DataProducto select o;

            if(!String.IsNullOrEmpty(searchString)){
                productos = productos.Where(s => s.Nombre.Contains(searchString) || s.Categoria.Contains(searchString));
            }

            productos = productos.Where(l => l.Estado.Contains("Stock"));
            return View(productos.ToList());
        }


        public async Task<IActionResult> Details(int? id){
            Producto objProduct = await _context.DataProducto.FindAsync(id);
            if(objProduct == null){
                return NotFound();
            }
            return View(objProduct);
        }

        public async Task<IActionResult> Add(int? id, string Talla = "S", int Cantidad = 1) // Talla por defecto
        {
            var userID = _userManager.GetUserName(User);
            if (userID == null)
            {
                ViewData["Message"] = "Por favor debe loguearse antes de agregar un producto";
                List<Producto> productos = new List<Producto>();
                return View("Index", productos);
            }
            else
            {
                var producto = await _context.DataProducto.FindAsync(id);
                Helper.SessionExtensions.Set<Producto>(HttpContext.Session, "MiUltimoProducto", producto);
                Carrito carrito = new Carrito();
                carrito.Producto = producto;
                carrito.Precio = producto.Precio;
                carrito.Talla = Talla; // Asignar la talla seleccionada (por defecto "S")
                carrito.Cantidad = Cantidad;
                carrito.UserID = userID;
                _context.Add(carrito);
                await _context.SaveChangesAsync();
                ViewData["Message"] = "Se Agrego al carrito";
                return RedirectToAction(nameof(Index));
            }
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}