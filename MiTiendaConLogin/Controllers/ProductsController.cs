using Microsoft.AspNetCore.Hosting; // Para subir archivos
using System.IO; // Para manejar rutas de archivos
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc; // <-- Corregido
using Microsoft.AspNetCore.Mvc.Rendering; // <-- Corregido
using Microsoft.EntityFrameworkCore; // <-- Corregido
using MiTiendaConLogin.Data;
using MiTiendaConLogin.Models;
using Microsoft.AspNetCore.Authorization; // ¡Importante!

namespace MiTiendaConLogin.Controllers
{
    // NO PONEMOS EL [Authorize] AQUÍ. LA CLASE ES PÚBLICA.
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment; // Asigna el nuevo servicio
        }

        // GET: Products (¡AHORA CON LÓGICA DE BÚSQUEDA!)
        // GET: Products (¡VERSIÓN CORREGIDA!)
        [AllowAnonymous]
        public async Task<IActionResult> Index(string searchString, int? categoryId)
        {
            // ▼▼▼ ¡AQUÍ ESTÁ EL CAMBIO! ▼▼▼
            // Usamos IQueryable<Product> en lugar de 'var' para que los
            // filtros .Where() no den error de conversión.
            IQueryable<Product> productsQuery = _context.Products.Include(p => p.Category);
            // ▲▲▲ ¡FIN DEL CAMBIO! ▲▲▲

            // Aplicar filtro de búsqueda (si existe)
            if (!String.IsNullOrEmpty(searchString))
            {
                productsQuery = productsQuery.Where(p =>
                    p.Name != null && p.Name.ToLower().Contains(searchString.ToLower())
                );
            }

            // Aplicar filtro de categoría (si existe)
            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            // Enviar la lista de categorías a la vista (para los botones)
            ViewData["Categories"] = await _context.Categories.ToListAsync();

            // Enviar los filtros actuales a la vista
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentCategory"] = categoryId;

            // Ejecutar la consulta final
            return View(await productsQuery.ToListAsync());
        }

        // GET: Products/Details/5
        [AllowAnonymous] // <-- Permite que cualquiera vea los detalles
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Products/Create
        [Authorize(Roles = "Admin,ProductManager")]
        public IActionResult Create()
        {
            // Añadimos esto: Carga las categorías y las envía a la vista
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        // POST: Products/Create
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = "Admin,ProductManager")]
public async Task<IActionResult> Create([Bind("Id,Name,Price,Stock,CategoryId")] Product product, IFormFile? ImageFile)
{
    // Quitamos ImageUrl del Bind, porque lo vamos a asignar nosotros.
    
    if (ModelState.IsValid)
    {
        // --- LÓGICA PARA SUBIR IMAGEN ---
        if (ImageFile != null && ImageFile.Length > 0)
        {
            // 1. Dónde guardar
            string wwwRootPath = _webHostEnvironment.WebRootPath;
            string imagesPath = Path.Combine(wwwRootPath, "images");
            
            // 2. Asegurarnos que la carpeta "images" exista
            if (!Directory.Exists(imagesPath))
            {
                Directory.CreateDirectory(imagesPath);
            }

            // 3. Crear un nombre de archivo único
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
            string filePath = Path.Combine(imagesPath, fileName);

            // 4. Guardar el archivo en el servidor
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await ImageFile.CopyToAsync(fileStream);
            }
            
            // 5. Guardar la RUTA en el producto
            product.ImageUrl = "/images/" + fileName; 
        }
        // --- FIN LÓGICA DE IMAGEN ---

        _context.Add(product);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
    
    // Si el modelo no es válido, volvemos a cargar las categorías
    ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
    return View(product);
}

        // GET: Products/Edit/5
        [Authorize(Roles = "Admin,ProductManager")] // <-- SOLO LOS MANAGERS PUEDEN EDITAR
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Carga las categorías y pre-selecciona la que el producto ya tiene
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);

            return View(product);
        }

        // POST: Products/Edit/5
[HttpPost]
[ValidateAntiForgeryToken]
[Authorize(Roles = "Admin,ProductManager")]
public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Price,Stock,CategoryId")] Product product, IFormFile? ImageFile)
{
    if (id != product.Id)
    {
        return NotFound();
    }

    if (ModelState.IsValid)
    {
        try
        {
            // --- LÓGICA PARA SUBIR IMAGEN (EN EDICIÓN) ---
            
            // 1. Buscamos el producto original en la BD
            var productFromDb = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            
            if (ImageFile != null && ImageFile.Length > 0)
            {
                // Si hay una IMAGEN NUEVA, la procesamos
                string wwwRootPath = _webHostEnvironment.WebRootPath;
                string imagesPath = Path.Combine(wwwRootPath, "images");
                
                // 2. Borrar la imagen antigua (si existía)
                if (!string.IsNullOrEmpty(productFromDb?.ImageUrl))
                {
                    var oldImagePath = Path.Combine(wwwRootPath, productFromDb.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // 3. Crear un nombre único y guardar la nueva
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                string filePath = Path.Combine(imagesPath, fileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await ImageFile.CopyToAsync(fileStream);
                }
                
                // 4. Asignar la RUTA NUEVA
                product.ImageUrl = "/images/" + fileName; 
            }
            else
            {
                // 5. Si NO hay imagen nueva, mantenemos la antigua
                product.ImageUrl = productFromDb?.ImageUrl;
            }
            // --- FIN LÓGICA DE IMAGEN ---

            _context.Update(product);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ProductExists(product.Id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }
        return RedirectToAction(nameof(Index));
    }
    
    // Si el modelo no es válido
    ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
    return View(product);
}

        // GET: Products/Delete/5
        [Authorize(Roles = "Admin,ProductManager")] // <-- SOLO LOS MANAGERS PUEDEN BORRAR
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,ProductManager")] // <-- SOLO LOS MANAGERS PUEDEN BORRAR
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}