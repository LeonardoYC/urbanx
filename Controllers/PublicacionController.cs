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
        private readonly string _facebookToken = "EAANQ567kCnsBO4Bb2BOJtY2E8rBXiI5oAB3vW4X2LFX2WEA1Nx6NwUTbuXHEwchmphomhyZAYOulglaxAZBnWiJnJjZAcStKvH2qQ9iuIDmzYYaunpyEpXERc99zOZACoSkUDfxl5MA5oSiaIFOUG73lBTvsvmOGVejwFTGY0iWI1VOZCqtoiVW1RZCDeqADSmos1H64F1ucL9SKke5QZDZD"; // Reemplaza con tu token

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
                var fb = new FacebookClient("EAANQ567kCnsBOz24kCdZAYQJynZC2Skv3qdFfPXzuDy4qdlUX77ZB81WcJyHXJCtGcUP2VaUAQhD4dpgtZC1GZB76Kf6dSBZB9HOSjJc8KacyUNrQhC2S9TFIoA9rmK1ZCWf4zoLCgPFuOZBBDiwxVa0ZCCiwe4bYl0SavZC2ILrwHp1VZAinBlHHnFcBo4i4loDhqH2KtbWS9jGBAE5ba7iGfn3xkZD"); // Token de la página

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
