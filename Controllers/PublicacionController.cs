using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Facebook;
using Newtonsoft.Json.Linq;

namespace urbanx.Controllers
{
    [Route("[controller]")]
    public class PublicacionController : Controller
    {
        private readonly ILogger<PublicacionController> _logger;
        private readonly string _facebookToken = "EAANQ567kCnsBO1pVzAALs794oyna6VehwYIDOG3EYWqscUrqk7JBWisVt1G69BoAswi8ZBmCtCbESmF31c60LiNsWCUcEGuUkxTkjFZCeI7SMBfbxUlPRy3SZB7sIJXXpXfS9GiryXN2N5aYjKYPHCTLk3cMQ5N2EGKt8ZCLIlVdeJC32TjoCVZC3wdjR4E8O8gypFaOTIuj779AIPgZDZD"; // Reemplaza con tu token

        public PublicacionController(ILogger<PublicacionController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {

            return View();
        }
        [HttpPost("Publicar")]
        public IActionResult Publicar(string titulo, string texto, string link, string comentario, string imagenUrl)
        {
            try
            {
                var fb = new FacebookClient("EAANQ567kCnsBO99Cf4ey6cf6mrG6VWVReqph8gZCMETaxouyuaFU2SF4oP8iap0dHLXFsVO93oAItyTUu2vhZAEDGXY7JfdaPa6n2wUctS8BJAVl11yVWeqaNe2uKFwZAZA0O8XYLjyQWLObSE6TJeLWZAVdZB5jN88isBDyZB3m7SYyKKZAW1jaYuzG5ONhhDAL7aLyUQHPZAePDRgAziNzanZAoZD"); // Token de la página

                var parameters = new
                {
                    message = $"{titulo}\n{texto}\n{link}\n{comentario}",
                    link = imagenUrl
                };

                dynamic result = fb.Post("103130604843458/feed", parameters);  // ID de la página

                // Obtenemos el post_id de la respuesta de Facebook
                string postId = result.id;
                string postUrl = $"https://www.facebook.com/{postId}";

                // Redirige automáticamente a la URL de la publicación
                return Redirect(postUrl);
            }
            catch (FacebookOAuthException ex)
            {
                _logger.LogError("Error al publicar en Facebook: " + ex.Message);
                // Devuelve la vista de error si ocurre un problema
                return View("Error");
            }
        }

        

        [HttpGet("Error")]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}
