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
        private readonly string _facebookToken = "EAAXymtlpH98BOxzZBShJzVTCMZAe6NYD8x4ZAfL4x1Pgc991MIabDdXGZA2kIdiIPRW06Kk0ivwmCZBJszAWrJd8aPpKnSoNoKHijsFNndJWym0ZAlZAUJilw6HALEIZCCW3Xn43D4uFcRCJ0aBCrfyozM2z68dExcSLxM71ytLZBPoREUz7yK7p0jrzm"; // Reemplaza con tu token

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
                var fb = new FacebookClient("EAAXymtlpH98BO1PkBKWkuaQorrPgyOWnDZCTlZAXVmUBJzsS1vtf2YHzs2tW88w8KFiZCQcqXrvjEpA0Vbfog02Nmfvuea9vTKzebrw1yD0Dr9pW19ZBtiyOhb3PFnvjn0A4NnqSLlUanxcIqvpBr8gPqkU6xX7tXvxHf6EnyOOc4GQTIDVr5ZA1GEE1CvoCu"); // Token de la página

                var parameters = new
                {
                    message = $"{titulo}\n{texto}\n{link}\n{comentario}",
                    link = imagenUrl
                };

                dynamic result = fb.Post("419279724605827/feed", parameters);  // ID de la página

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