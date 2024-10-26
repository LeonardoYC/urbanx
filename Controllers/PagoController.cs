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
        public async Task<IActionResult> Pagar(string stripeToken, string titular, Pago pago)
        {
            var customers = new Stripe.CustomerService();
            var charges = new Stripe.ChargeService();

            // Crear un cliente de Stripe
            var customer = await customers.CreateAsync(new CustomerCreateOptions
            {
                Email = User.Identity?.Name,
                Source = stripeToken,
            });

            try
            {
                pago.PaymentDate = DateTime.UtcNow;
                _context.Add(pago);

                // Obtener los items en el carrito
                var itemsCarrito = await _context.DataItemCarrito
                    .Include(p => p.Producto)
                    .Where(s => s.UserID.Equals(pago.UserID) && s.Estado.Equals("PENDIENTE"))
                    .ToListAsync();

                // Crear un nuevo pedido
                var pedido = new Pedido
                {
                    UserID = pago.UserID,
                    Total = pago.MontoTotal,
                    pago = pago,
                    Status = "PENDIENTE"
                };
                _context.Add(pedido);

                // Calcular el monto total para el cargo de Stripe
                var totalAmount = itemsCarrito.Sum(item => item.Precio * item.Cantidad);

                // Crear el cargo en Stripe
                var charge = await charges.CreateAsync(new ChargeCreateOptions
                {
                    Amount = (long)(totalAmount * 100), // Convertir a centavos
                    Description = "Compra de " + titular,
                    Currency = "usd",
                    Customer = customer.Id
                });

                // Guardar detalles del pedido
                var itemsPedido = itemsCarrito.Select(item => new DetallePedido
                {
                    pedido = pedido,
                    Precio = item.Precio,
                    Producto = item.Producto,
                    Cantidad = item.Cantidad
                }).ToList();

                // Actualizar el estado de los items en el carrito
                itemsCarrito.ForEach(item => item.Estado = "PROCESADO");

                _context.AddRange(itemsPedido);
                _context.UpdateRange(itemsCarrito);
                await _context.SaveChangesAsync();

                TempData["UltimoPedidoId"] = pedido.ID;
                ViewData["Message"] = $"El pago se ha registrado y su pedido nro {pedido.ID} está en camino.";

                // Crear un nuevo modelo Pago con monto 0
                var nuevoPago = new Pago
                {
                    UserID = _userManager.GetUserName(User),
                    MontoTotal = 0
                };

                return View("Create", nuevoPago);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar el pago");
                ViewData["Message"] = "Ocurrió un error al procesar el pago. Por favor, intente nuevamente.";
                return View("Create", pago);
            }
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