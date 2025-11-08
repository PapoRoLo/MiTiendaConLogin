using Microsoft.AspNetCore.Mvc;
using MiTiendaConLogin.Models;
using MiTiendaConLogin.Data;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiTiendaConLogin.Controllers
{
    // Simple 'mini-modelo' para el carrito
    public class CartItem
    {
        public int ProductId { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal => Price * Quantity; // Propiedad calculada
    }

    // ViewModel para la página del Carrito
    public class CartViewModel
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();
        public decimal Total { get; set; }
    }

    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- 1. MOSTRAR EL CARRITO ---
        [HttpGet]
        public IActionResult Index()
        {
            var cart = GetCartFromSession();
            var viewModel = new CartViewModel
            {
                Items = cart,
                Total = cart.Sum(item => item.Subtotal)
            };
            return View(viewModel);
        }

        // --- 2. AÑADIR AL CARRITO ---
        [HttpPost]
        public async Task<IActionResult> AddToCart(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) { return NotFound(); }

            var cart = GetCartFromSession();
            var existingItem = cart.FirstOrDefault(item => item.ProductId == id);

            if (existingItem != null)
            {
                existingItem.Quantity++;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    Price = product.Price, 
                    Quantity = 1
                });
            }
            SaveCartToSession(cart);
            return RedirectToAction("Index", "Cart");
        }

        // --- 3. QUITAR DEL CARRITO (MODIFICADO PARA AJAX) ---
        [HttpPost]
        public IActionResult RemoveFromCart(int id)
        {
            var cart = GetCartFromSession();
            var itemToRemove = cart.FirstOrDefault(item => item.ProductId == id);

            if (itemToRemove != null)
            {
                cart.Remove(itemToRemove);
                SaveCartToSession(cart);
            }

            // Devuelve el nuevo total para que JavaScript actualice la página
            var newTotal = cart.Sum(item => item.Subtotal);
            return Json(new { success = true, newTotal = newTotal.ToString("C"), removedId = id });
        }

        // --- 4. ACTUALIZAR CANTIDAD (MODIFICADO PARA AJAX) ---
        [HttpPost]
        public IActionResult UpdateQuantity(int id, int quantity)
        {
            var cart = GetCartFromSession();
            var itemToUpdate = cart.FirstOrDefault(item => item.ProductId == id);

            if (itemToUpdate == null)
            {
                return Json(new { success = false, message = "Item not found" });
            }

            if (quantity <= 0)
            {
                // Si la bajan a 0, la eliminamos
                cart.Remove(itemToUpdate);
                SaveCartToSession(cart);
                
                var newTotalAfterRemove = cart.Sum(item => item.Subtotal);
                // Usamos 'removedId' para que JS sepa que debe borrar la fila
                return Json(new { success = true, newTotal = newTotalAfterRemove.ToString("C"), removedId = id });
            }
            else
            {
                // Si es > 0, solo actualizamos
                itemToUpdate.Quantity = quantity;
                SaveCartToSession(cart);

                // Devuelve los nuevos subtotales y totales para que JS actualice la página
                var newSubtotal = itemToUpdate.Subtotal;
                var newTotal = cart.Sum(item => item.Subtotal);

                return Json(new { 
                    success = true, 
                    newSubtotal = newSubtotal.ToString("C"), 
                    newTotal = newTotal.ToString("C") 
                });
            }
        }

        // --- Métodos Ayudantes (Helpers) para manejar la Sesión ---
        private List<CartItem> GetCartFromSession()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(cartJson)) { return new List<CartItem>(); }
            return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
        }

        private void SaveCartToSession(List<CartItem> cart)
        {
            var cartJson = JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString("Cart", cartJson);
        }
    }
}