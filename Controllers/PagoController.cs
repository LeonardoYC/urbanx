using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using urbanx.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using urbanx.Data;
using urbanx.ViewModels;
using Rotativa.AspNetCore;

namespace urbanx.Controllers
{
    public class PagoController : Controller
    {
        private readonly ILogger<PagoController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;

        public PagoController(ILogger<PagoController> logger,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context)
        {
            _logger = logger;
            _userManager = userManager;
            _context = context;
        }

        public IActionResult Create(Decimal monto)
        {
            Pago pago = new Pago();
            pago.UserID = _userManager.GetUserName(User);
            pago.MontoTotal = monto;
            return View(pago);
        }

        [HttpPost]
        public async Task<IActionResult> Pagar(Pago pago)
        {
            try
            {
                pago.PaymentDate = DateTime.UtcNow;
                _context.Add(pago);

                var itemsCarrito = await _context.DataItemCarrito
                    .Include(p => p.Producto)
                    .Where(s => s.UserID.Equals(pago.UserID) && s.Estado.Equals("PENDIENTE"))
                    .ToListAsync();

                Pedido pedido = new Pedido();
                pedido.UserID = pago.UserID;
                pedido.Total = pago.MontoTotal;
                pedido.pago = pago;
                pedido.Status = "PENDIENTE";
                _context.Add(pedido);

                List<DetallePedido> itemsPedido = new List<DetallePedido>();

                foreach (var item in itemsCarrito)
                {
                    DetallePedido detallePedido = new DetallePedido
                    {
                        pedido = pedido,
                        Precio = item.Precio,
                        Producto = item.Producto,
                        Cantidad = item.Cantidad
                    };
                    itemsPedido.Add(detallePedido);
                    item.Estado = "PROCESADO";
                }

                _context.AddRange(itemsPedido);
                _context.UpdateRange(itemsCarrito);
                await _context.SaveChangesAsync();

                // Guardar el ID del pedido en TempData para poder descargarlo después
                TempData["UltimoPedidoId"] = pedido.ID;
                ViewData["Message"] = "El pago se ha registrado y su pedido nro " + pedido.ID + " esta en camino";

                return View("Create");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar el pago");
                ViewData["Message"] = "Ocurrió un error al procesar el pago. Por favor, intente nuevamente.";
                return View("Create", pago);
            }
        }

        public async Task<IActionResult> DescargarPDF()
        {
            try
            {
                var userId = _userManager.GetUserName(User);
                var ultimoPedidoId = TempData["UltimoPedidoId"];

                var itemsPedido = await _context.DetallePedido
                    .Include(d => d.Producto)
                    .Where(d => d.pedido.UserID == userId)
                    .OrderByDescending(d => d.pedido.ID)
                    .Take(1)
                    .ToListAsync();

                if (!itemsPedido.Any())
                {
                    return RedirectToAction("Create");
                }

                var resumenItems = itemsPedido.Select(item => new CarritoResumenViewModel
                {
                    Producto = item.Producto?.Nombre ?? "Sin nombre",
                    CantProdu = item.Cantidad,
                    Precio = item.Precio
                }).ToList();

                return new ViewAsPdf("ImprimirPago", resumenItems)  // Change "CarritoResumenPDF" to "ImprimirPago"
                {
                    FileName = $"Pedido_{ultimoPedidoId ?? "resumen"}.pdf",
                    PageSize = Rotativa.AspNetCore.Options.Size.A4,
                    PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar el PDF");
                TempData["ErrorMessage"] = "Error al generar el PDF. Por favor, intente nuevamente.";
                return RedirectToAction("Create");
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}