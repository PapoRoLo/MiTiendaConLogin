using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MiTiendaConLogin.Models;
using MiTiendaConLogin.Data; // <-- Importante: para la base de datos
using Microsoft.AspNetCore.Authorization; // <-- Importante: para el login

namespace MiTiendaConLogin.Controllers
{
    [Authorize] // Protege todas las acciones de este controlador
    public class HomeController : Controller
    {
        // Variable para guardar la conexión a la base de datos
        private readonly ApplicationDbContext _context;

        // "Inyección de dependencias": Pedimos la base de datos en el constructor
        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // ¡Obtenemos los productos DESDE LA BASE DE DATOS!
            var products = _context.Products.ToList();

            // Si la base de datos está vacía, añadamos los productos de ejemplo
            if (!products.Any())
            {
                _context.Products.AddRange(
                    new Product { Name = "SSD 1TB", Price = 37000m, ImageUrl = "" },
                    new Product { Name = "Fuente BeQuiet! 850W", Price = 85000m, ImageUrl = "" },
                    new Product { Name = "Cable Cat 7 - 15m", Price = 8000m, ImageUrl = "" }
                );
                _context.SaveChanges(); // Guardamos en la BD
                products = _context.Products.ToList(); // Los volvemos a leer
            }

            return View(products); // Le pasamos los productos a la vista
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // Le dice a la aplicación que renderice la vista "Privacy.cshtml".
        public IActionResult Privacy()
        {
            return View();
        }
    }
}