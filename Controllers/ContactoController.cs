using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using urbanx.Data;
using urbanx.Models;
using ClasificacionModelo;


namespace urbanx.Controllers
{

    public class ContactoController : Controller
    {
        private readonly ILogger<ContactoController> _logger;
        private readonly ApplicationDbContext _context;

        private readonly HttpClient _httpClient;

        public ContactoController(ILogger<ContactoController> logger, ApplicationDbContext context, HttpClient httpClient)
        {
            _logger = logger;
            _context = context;
            _httpClient = httpClient;
        }


        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> EnviarMensaje(Contacto objContacto)
        {
            _logger.LogDebug("Ingreso a Enviar Mensaje");

            MLModelTextClassification.ModelInput sampleData = new MLModelTextClassification.ModelInput()
            {
                Comentario = objContacto.Message
            };
            
            MLModelTextClassification.ModelOutput output = MLModelTextClassification.Predict(sampleData);

            var sortedScoresWithLabel = MLModelTextClassification.PredictAllLabels(sampleData);
            var scoreKeyFirst = sortedScoresWithLabel.ToList()[0].Key;
            var scoreValueFirst = sortedScoresWithLabel.ToList()[0].Value;
            var scoreKeySecond = sortedScoresWithLabel.ToList()[1].Key;
            var scoreValueSecond = sortedScoresWithLabel.ToList()[1].Value;

            if (scoreKeyFirst == "1")
            {
                objContacto.Category = "Positivo";
            }
            else
            {
                objContacto.Category = "Negativo";
            }

            
            Console.WriteLine($"{scoreKeyFirst,-40}{scoreValueFirst,-20}");
            Console.WriteLine($"{scoreKeySecond,-40}{scoreValueSecond,-20}");

            // 1. Guardar datos en la base de datos
            _context.Add(objContacto);
            _context.SaveChanges();

            // 2. Enviar datos a Formspree
            var formData = new
            {
                name = objContacto.Name,
                email = objContacto.Email,
                phone = objContacto.Phone,
                message = objContacto.Message
            };

            var formContent = new StringContent(JsonConvert.SerializeObject(formData), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync("https://formspree.io/f/xzzbppog", formContent);
                if (response.IsSuccessStatusCode)
                {
                    ViewData["Message"] = "Se registró el contacto y el comentario ha tenido un sentimiento. " + objContacto.Category;
                    
                    // Limpia los datos creando un nuevo objeto Contacto vacío
                    ModelState.Clear();
                    objContacto = new Contacto();
                }
                else
                {
                    _logger.LogError("Error al enviar el mensaje a Formspree: " + response.ReasonPhrase);
                    ViewData["Message"] = "Mensaje guardado en la base de datos, pero hubo un error al enviar el correo.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Excepción al enviar el mensaje: " + ex.Message);
                ViewData["Message"] = "Mensaje guardado en la base de datos, pero hubo un error al enviar el correo.";
            }

            return View("Index", objContacto);  // Asegúrate de pasar el objeto a la vista
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}