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
using urbanx.Models;
using urbanx.ViewModels;

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
                ViewData["Message"] = $"El pago se ha registrado y su pedido nro {pedido.ID} está en camino.";

                return View("Create");
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

        public async Task<IActionResult> DescargarPDF()
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
                    try
                    {
                        await EnviarEmailConPdf(user.Email, ultimoPedido.ID.ToString(), pdfBytes);
                        TempData["Message"] = "El resumen de tu pedido ha sido enviado a tu correo electrónico.";
                    }
                    catch
                    {
                        TempData["ErrorMessage"] = "No se pudo enviar el email, pero puedes descargar el PDF.";
                    }
                }

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