using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Rotativa.AspNetCore;
using urbanx.Data;
using Stripe;
using urbanx.Models;
using urbanx.ViewModels;
using Stripe.TestHelpers;
using Stripe.BillingPortal;
using Stripe.Checkout;

namespace urbanx.Controllers
{
    public class PagoController : Controller
    {
        private readonly ILogger<PagoController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public PagoController(
            ILogger<PagoController> logger,
            UserManager<IdentityUser> userManager,
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _logger = logger;
            _userManager = userManager;
            _context = context;
            _configuration = configuration;
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        public IActionResult Create(decimal monto)
        {
            var pago = new Pago
            {
                UserID = _userManager.GetUserName(User),
                MontoTotal = monto
            };
            return View(pago);
        }

        [HttpPost]
        public async Task<IActionResult> CrearSesionCheckout([FromBody] Pago pago)
        {
            try
            {
                var domain = $"{Request.Scheme}://{Request.Host}";

                // Obtener los items del carrito
                var itemsCarrito = await _context.DataItemCarrito
                    .Include(p => p.Producto)
                    .Where(s => s.UserID.Equals(pago.UserID) && s.Estado.Equals("PENDIENTE"))
                    .ToListAsync();

                // Crear los line items para Stripe
                var lineItems = new List<SessionLineItemOptions>();
                foreach (var item in itemsCarrito)
                {
                    lineItems.Add(new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            UnitAmount = (long)(item.Precio * 100), // Convertir a centavos
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Producto.Nombre,
                                Description = item.Producto.Descripcion
                            }
                        },
                        Quantity = item.Cantidad
                    });
                }

                // Crear la sesión de Checkout
                var options = new Stripe.Checkout.SessionCreateOptions
                {
                    LineItems = lineItems,
                    Mode = "payment",
                    SuccessUrl = domain + $"/Pago/PagoExitoso?session_id={{CHECKOUT_SESSION_ID}}",
                    CancelUrl = domain + "/Pago/PagoCancelado",
                    CustomerEmail = User.Identity?.Name,
                    PaymentMethodTypes = new List<string> { "card" }
                };

                var service = new Stripe.Checkout.SessionService();
                var session = await service.CreateAsync(options);

                return Json(new { id = session.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear la sesión de Checkout");
                return BadRequest(new { error = "Error al crear la sesión de pago" });
            }
        }

        public async Task<IActionResult> PagoExitoso(string session_id)
        {
            try
            {
                var sessionService = new Stripe.Checkout.SessionService();
                var session = await sessionService.GetAsync(session_id);

                if (session.PaymentStatus == "paid")
                {
                    var pago = new Pago
                    {
                        UserID = _userManager.GetUserName(User),
                        PaymentDate = DateTime.UtcNow,
                        MontoTotal = (decimal)(session.AmountTotal ?? 0) / 100 // Convertir de centavos
                    };
                    _context.Add(pago);

                    var itemsCarrito = await _context.DataItemCarrito
                        .Include(p => p.Producto)
                        .Where(s => s.UserID.Equals(pago.UserID) && s.Estado.Equals("PENDIENTE"))
                        .ToListAsync();

                    var pedido = new Pedido
                    {
                        UserID = pago.UserID,
                        Total = pago.MontoTotal,
                        pago = pago,
                        Status = "PENDIENTE"
                    };
                    _context.Add(pedido);

                    var itemsPedido = itemsCarrito.Select(item => new DetallePedido
                    {
                        pedido = pedido,
                        Precio = item.Precio,
                        Producto = item.Producto,
                        Cantidad = item.Cantidad
                    }).ToList();

                    itemsCarrito.ForEach(item => item.Estado = "PROCESADO");

                    _context.AddRange(itemsPedido);
                    _context.UpdateRange(itemsCarrito);
                    await _context.SaveChangesAsync();

                    TempData["UltimoPedidoId"] = pedido.ID;
                    TempData["SuccessMessage"] = $"El pago se ha registrado y su pedido nro {pedido.ID} está en camino.";
                }

                return RedirectToAction("Create", new { monto = 0 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar el pago exitoso");
                TempData["ErrorMessage"] = "Error al procesar el pago. Por favor, contacte al soporte.";
                return RedirectToAction("Create");
            }
        }

        public IActionResult PagoCancelado()
        {
            TempData["ErrorMessage"] = "El pago fue cancelado.";
            return RedirectToAction("Create");
        }

        private async Task EnviarEmailConPdf(string emailDestino, string numeroPedido, byte[] pdfBytes)
        {
            try
            {
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var password = _configuration["EmailSettings:Password"];
                var host = _configuration["EmailSettings:Host"];
                var port = int.Parse(_configuration["EmailSettings:Port"]);

                using (var mailMessage = new MailMessage())
                {
                    mailMessage.From = new MailAddress(fromEmail);
                    mailMessage.To.Add(emailDestino);
                    mailMessage.Subject = $"Resumen de tu pedido #{numeroPedido} - UrbanX";
                    mailMessage.Body = $@"
                        <h2>¡Gracias por tu compra en UrbanX!</h2>
                        <p>Adjunto encontrarás el resumen de tu pedido #{numeroPedido}.</p>
                        <p>Si tienes alguna pregunta, no dudes en contactarnos.</p>
                        <p>¡Gracias por confiar en UrbanX!</p>";
                    mailMessage.IsBodyHtml = true;

                    using (var ms = new MemoryStream(pdfBytes))
                    {
                        var attachment = new Attachment(ms, $"Pedido_{numeroPedido}.pdf", "application/pdf");
                        mailMessage.Attachments.Add(attachment);

                        using (var smtpClient = new SmtpClient(host, port))
                        {
                            smtpClient.Credentials = new NetworkCredential(fromEmail, password);
                            smtpClient.EnableSsl = true;
                            await smtpClient.SendMailAsync(mailMessage);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar el email");
                throw;
            }
        }

        public async Task<IActionResult> EnviarPDFPorCorreo()
        {
            try
            {
                var userId = _userManager.GetUserName(User);
                var user = await _userManager.FindByNameAsync(userId);

                var ultimoPedido = await _context.Pedido
                    .Where(p => p.UserID == userId)
                    .OrderByDescending(p => p.ID)
                    .FirstOrDefaultAsync();

                if (ultimoPedido == null)
                {
                    TempData["ErrorMessage"] = "No se encontró ningún pedido.";
                    return RedirectToAction("Create");
                }

                var itemsPedido = await _context.DetallePedido
                    .Include(d => d.Producto)
                    .Where(d => d.pedido.ID == ultimoPedido.ID)
                    .ToListAsync();

                var resumenItems = itemsPedido.Select(item => new CarritoResumenViewModel
                {
                    Producto = item.Producto?.Nombre ?? "Sin nombre",
                    CantProdu = item.Cantidad,
                    Precio = item.Precio,
                    NumeroPedido = ultimoPedido.ID
                }).ToList();

                var pdf = new ViewAsPdf("ImprimirPago", resumenItems)
                {
                    FileName = $"Pedido_{ultimoPedido.ID}.pdf",
                    PageSize = Rotativa.AspNetCore.Options.Size.A4,
                    PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait
                };

                var pdfBytes = await pdf.BuildFile(ControllerContext);

                if (user?.Email != null)
                {
                    await EnviarEmailConPdf(user.Email, ultimoPedido.ID.ToString(), pdfBytes);
                    TempData["Message"] = "El resumen de tu pedido ha sido enviado a tu correo electrónico.";
                }
                else
                {
                    TempData["ErrorMessage"] = "No se pudo enviar el correo, el usuario no tiene email registrado.";
                }

                return RedirectToAction("Create");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar el email");
                TempData["ErrorMessage"] = "Error al enviar el correo. Por favor, intenta nuevamente.";
                return RedirectToAction("Create");
            }
        }


        public async Task<IActionResult> DescargarPDF()
        {
            try
            {
                var userId = _userManager.GetUserName(User);
                var ultimoPedido = await _context.Pedido
                    .Where(p => p.UserID == userId)
                    .OrderByDescending(p => p.ID)
                    .FirstOrDefaultAsync();

                if (ultimoPedido == null)
                {
                    return RedirectToAction("Create");
                }

                var itemsPedido = await _context.DetallePedido
                    .Include(d => d.Producto)
                    .Where(d => d.pedido.ID == ultimoPedido.ID)
                    .ToListAsync();

                var resumenItems = itemsPedido.Select(item => new CarritoResumenViewModel
                {
                    Producto = item.Producto?.Nombre ?? "Sin nombre",
                    CantProdu = item.Cantidad,
                    Precio = item.Precio,
                    NumeroPedido = ultimoPedido.ID
                }).ToList();

                var pdf = new ViewAsPdf("ImprimirPago", resumenItems)
                {
                    FileName = $"Pedido_{ultimoPedido.ID}.pdf",
                    PageSize = Rotativa.AspNetCore.Options.Size.A4,
                    PageOrientation = Rotativa.AspNetCore.Options.Orientation.Portrait
                };

                var pdfBytes = await pdf.BuildFile(ControllerContext);

                return File(pdfBytes, "application/pdf", $"Pedido_{ultimoPedido.ID}.pdf");
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