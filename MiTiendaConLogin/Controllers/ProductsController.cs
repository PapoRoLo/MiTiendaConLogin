using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MiTiendaConLogin.Data;
using MiTiendaConLogin.Models;
using Microsoft.AspNetCore.Authorization; // <-- AÑADIDO: Para la seguridad
using Microsoft.AspNetCore.Hosting; // <-- AÑADIDO: Para saber dónde está wwwroot
using System.IO; // <-- AÑADIDO: Para manejar archivos

namespace MiTiendaConLogin.Controllers
{
    // Solo el Admin O el ProductManager pueden entrar aquí
    [Authorize(Roles = "Admin,ProductManager")] 
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment; // <-- AÑADIDO

        // Constructor modificado
        public ProductsController(ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment; // <-- AÑADIDO
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            return View(await _context.Products.ToListAsync());
        }

        // GET: Products/Details/5
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
        public IActionResult Create()
        {
            return View();
        }

        // POST: Products/Create
        // ¡¡ESTA ES LA LÓGICA IMPORTANTE!!
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Price,Stock,ImageFile")] Product product)
        {
            if (ModelState.IsValid)
            {
                // --- Lógica de subida de imagen ---
                if (product.ImageFile != null)
                {
                    // 1. Dónde guardar (wwwroot/images)
                    string wwwRootPath = _hostEnvironment.WebRootPath;
                    string folderPath = Path.Combine(wwwRootPath, "images");

                    // 2. Nombre único para el archivo
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(product.ImageFile.FileName);
                    string filePath = Path.Combine(folderPath, fileName);

                    // 3. Guardar el archivo en el disco
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await product.ImageFile.CopyToAsync(fileStream);
                    }

                    // 4. Guardar la RUTA en la base de datos
                    product.ImageUrl = "/images/" + fileName; 
                }
                // --- Fin de la lógica ---

                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Products/Edit/5
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
            return View(product);
        }

        // POST: Products/Edit/5
        // ¡¡LÓGICA IMPORTANTE DE EDITAR!!
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Price,Stock,ImageFile,ImageUrl")] Product productDataFromForm)
        {
            if (id != productDataFromForm.Id)
            {
                return NotFound();
            }

            // 1. Buscamos el producto ORIGINAL en la base de datos
            var productToUpdate = await _context.Products.FindAsync(id);

            if (productToUpdate == null)
            {
                return NotFound();
            }

            // 2. Comprobamos si el modelo es válido
            if (ModelState.IsValid)
            {
                try
                {
                    // 3. Copiamos los valores simples del formulario al producto de la BD
                    productToUpdate.Name = productDataFromForm.Name;
                    productToUpdate.Price = productDataFromForm.Price;
                    productToUpdate.Stock = productDataFromForm.Stock; // <-- ¡ESTO ARREGLA EL STOCK!

                    // 4. Lógica de subida de imagen (si se sube una nueva)
                    if (productDataFromForm.ImageFile != null)
                    {
                        // Borrar la imagen antigua (si existe)
                        if (!string.IsNullOrEmpty(productToUpdate.ImageUrl))
                        {
                            var oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, productToUpdate.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        // Guardar la imagen nueva (mismo código que Create)
                        string wwwRootPath = _hostEnvironment.WebRootPath;
                        string folderPath = Path.Combine(wwwRootPath, "images");
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(productDataFromForm.ImageFile.FileName);
                        string filePath = Path.Combine(folderPath, fileName);
                        
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await productDataFromForm.ImageFile.CopyToAsync(fileStream);
                        }
                        // Actualizamos la URL en el producto de la BD
                        productToUpdate.ImageUrl = "/images/" + fileName; 
                    }
                    // Si no se sube un archivo nuevo, no se hace nada y la ImageUrl antigua se conserva.

                    // 5. Guardamos los cambios
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Products.Any(e => e.Id == productToUpdate.Id))
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
            
            // Si el modelo no es válido, volvemos a la vista
            return View(productToUpdate);
        }

        // GET: Products/Delete/5
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
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                // --- Borrar la imagen del disco ---
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    var imagePath = Path.Combine(_hostEnvironment.WebRootPath, product.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }
                // --- Fin de la lógica ---
                
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