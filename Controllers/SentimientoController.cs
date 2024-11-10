using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using urbanx.Data;
using Microsoft.EntityFrameworkCore;


namespace urbanx.Controllers
{
    public class SentimientoController : Controller
    {
        private readonly ILogger<SentimientoController> _logger;
        private readonly ApplicationDbContext _context;

        public SentimientoController(ILogger<SentimientoController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.DataContacto.ToListAsync());
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}