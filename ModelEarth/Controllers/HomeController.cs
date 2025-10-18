using Microsoft.AspNetCore.Mvc;
using ModelEarth.Models;
using Npgsql;
using System.Diagnostics;
using System.IO;

namespace ModelEarth.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _env;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment env)
        {
            _env = env;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var connections = LoadConnectionsFromCookie();
            return View(connections);
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
