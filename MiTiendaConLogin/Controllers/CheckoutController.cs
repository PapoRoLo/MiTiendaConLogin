using Microsoft.AspNetCore.Mvc;
using MiTiendaConLogin.Data;
using MiTiendaConLogin.Models;
using MiTiendaConLogin.Controllers; // Para poder usar CartItem y CartViewModel
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using MiTiendaConLogin.Services; // Para IEmailSender

namespace MiTiendaConLogin.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAppEmailSender _emailSender;
        private readonly IConfiguration _configuration; // Para leer el correo del Admin

        public CheckoutController(ApplicationDbContext context, IAppEmailSender emailSender, IConfiguration configuration)
        {
            _context = context;
            _emailSender = emailSender;
            _configuration = configuration;
        }


        // --- MÉTODOS AYUDANTES (LOS MISMOS DE CARTCONTROLLER) ---
        private List<CartItem> GetCartFromSession()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            if (string.IsNullOrEmpty(cartJson)) { return new List<CartItem>(); }
            return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
        }

        private void ClearCartSession()
        {
            HttpContext.Session.Remove("Cart");
        }
        
        // --- FASE 1: MOSTRAR EL FORMULARIO ---
        [HttpGet]
        public IActionResult Index()
        {
            var cart = GetCartFromSession();
            if (cart.Count == 0)
            {
                // No dejes que nadie entre al checkout con un carrito vacío
                return RedirectToAction("Index", "Cart");
            }
            
            // Pasamos un CheckoutViewModel vacío al formulario
            return View(new CheckoutViewModel());
        }

        // --- FASE 2: PROCESAR EL PEDIDO ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CheckoutViewModel checkoutModel)
        {
            var cart = GetCartFromSession();
            if (cart.Count == 0)
            {
                // Carrito vacío, vuelve al inicio
                return RedirectToAction("Index", "Products");
            }

            if (!ModelState.IsValid)
            {
                // Si el formulario no es válido (ej. falta el email),
                // muestra el formulario de nuevo con los errores
                return View(checkoutModel);
            }

            // ¡TODO VÁLIDO! Procedemos a crear la Orden en la Base de Datos

            // 1. Crear el objeto 'Order' principal
            var order = new Order
            {
                CustomerEmail = checkoutModel.Email,
                OrderDate = DateTime.Now, // ¡La hora del servidor, no del cliente!
                RequestedDeliveryDate = checkoutModel.RequestedDeliveryDate,
                Notes = checkoutModel.Notes,
                Status = "Pendiente", // ¡El estado inicial clave!
                Total = cart.Sum(item => item.Subtotal),

                // 2. Crear la lista de 'OrderDetails'
                OrderDetails = new List<OrderDetail>()
            };

            // 3. Llenar la lista de 'OrderDetails' con los items del carrito
            foreach (var cartItem in cart)
            {
                order.OrderDetails.Add(new OrderDetail
                {
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    Price = cartItem.Price // Guardamos el precio al momento de la compra
                });
            }

            // 4. Guardar todo en la Base de Datos
            _context.Orders.Add(order); // EF es lo bastante inteligente para guardar la Orden Y sus Detalles
            await _context.SaveChangesAsync();

            // --- 5. ¡AQUÍ ENVIAMOS LOS CORREOS! ---
            try
            {
                // 5a. Correo para el Cliente
                string customerSubject = $"Confirmación de Pedido #{order.Id} - Corteza Dorada";
                string customerHtmlBody = $@"
                <h1>¡Gracias por tu pedido, {checkoutModel.Name}!</h1>
                <p>Hemos recibido tu pedido con el número <strong>#{order.Id}</strong>.</p>
                <p>Nos pondremos en contacto contigo pronto al teléfono {checkoutModel.Phone} o a este correo para coordinar la entrega y el pago.</p>
                <hr>
                <p><strong>Total del Pedido:</strong> {order.Total.ToString("C")}</p>
                <p><strong>Fecha Solicitada:</strong> {order.RequestedDeliveryDate.ToShortDateString()}</p>
                <p><strong>Notas:</strong> {order.Notes}</p>";

                await _emailSender.SendEmailAsync(checkoutModel.Email, customerSubject, "Tu pedido ha sido recibido", customerHtmlBody);

                // 5b. Correo para el Admin/OrderManager
                string adminEmail = _configuration["SendGrid:AdminNotificationEmail"];
                if (!string.IsNullOrEmpty(adminEmail))
                {
                    string adminSubject = $"¡Nuevo Pedido Recibido! #{order.Id}";
                    string adminHtmlBody = $@"
                    <h1>¡Nuevo Pedido!</h1>
                    <p>Se ha recibido un nuevo pedido (#{order.Id}) en la plataforma.</p>
                    <p><strong>Cliente:</strong> {checkoutModel.Name} ({checkoutModel.Email})</p>
                    <p><strong>Total:</strong> {order.Total.ToString("C")}</p>
                    <p><strong>Notas:</strong> {order.Notes}</p>
                    <p>Por favor, ingresa al panel de 'Pedidos Activos' para gestionarlo.</p>";

                    await _emailSender.SendEmailAsync(adminEmail, adminSubject, "Nuevo Pedido Recibido", adminHtmlBody);
                }
            }
            catch (Exception ex)
            {
                // Opcional: Registrar el error si el correo falla
                // Por ahora, no detenemos al cliente si el correo falla.
            }
            // --- FIN DEL ENVÍO DE CORREOS ---

            // 6. Limpiar el carrito de la sesión
            ClearCartSession();

            // 7. Redirigir a una página de "Gracias"
            return RedirectToAction("Success", new { id = order.Id });
        }
        // --- FASE 3: PÁGINA DE ÉXITO ---
        [HttpGet]
        public IActionResult Success(int id)
        {
            ViewBag.OrderId = id;
            return View();
        }
    }
}