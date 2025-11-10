using Microsoft.AspNetCore.Mvc;
using MiTiendaConLogin.Data;
using MiTiendaConLogin.Models;
using MiTiendaConLogin.Controllers; // Para poder usar CartItem y CartViewModel
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using MiTiendaConLogin.Services; // Para IEmailSender
using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.EntityFrameworkCore; 

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
                OrderDate = DateTime.UtcNow, // ¡La hora del servidor, no del cliente!
                RequestedDeliveryDate = checkoutModel.RequestedDeliveryDate.ToUniversalTime(),
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

            // Volvemos a cargar los detalles del pedido, pero INCLUYENDO el Producto
            var orderDetailsWithProducts = await _context.OrderDetails
                .Where(d => d.OrderId == order.Id)
                .Include(d => d.Product)
                .ToListAsync();
    

            // --- 5. ¡AQUÍ ENVIAMOS LOS CORREOS (CON PLANTILLAS)! ---
            try
            {
                // 5a. Correo para el Cliente (Usando tu Plantilla)

                // Lee el ID de la plantilla desde appsettings
                string? templateId = _configuration["SendGrid:TemplateIdCliente"];

                // Prepara el objeto de datos. ¡Los nombres deben coincidir
                // EXACTAMENTE con tus variables en SendGrid!
                var templateDataCliente = new
                {
                    // Tus variables de la captura
                    fecha_pedido = order.OrderDate.ToShortDateString(),
                    nombre_cliente = checkoutModel.Name,
                    email_cliente = checkoutModel.Email,
                    numero_pedido = order.Id.ToString(),
                    monto_total = order.Total.ToString("C"),

                    // Estas son variables que yo asumí, puedes borrarlas
                    // si no las usaste en tu plantilla
                    nombre_empleado = "Equipo de Corteza Dorada",

                    // --- Para el bucle de productos (si lo tienes) ---
                    // SendGrid usa "items" para el bucle {{#each items}}
                    items = orderDetailsWithProducts.Select(d => new
                    {
                        producto = d.Product?.Name ?? "Producto",
                        cantidad = d.Quantity,
                        precio = d.Price.ToString("C")
                    }).ToList()
                };

                // ¡Un buen asunto para el correo!
                string customerSubject = $"Tu pedido #{order.Id} de Corteza Dorada está confirmado";

                // Le pasamos el asunto al método de SendGrid (aunque la plantilla lo puede sobreescribir)
                // ¡No, espera! El método CreateDynamicTemplateEmail no usa asunto. El asunto se define en la plantilla.
                // ¡Mejor! Vamos a crear el correo manualmente para poner asunto Y plantilla.

                // --- Corrección: Creación manual para Asunto + Plantilla ---
                var from = new EmailAddress(_configuration["SendGrid:FromEmail"], _configuration["SendGrid:FromName"]);
                var to = new EmailAddress(checkoutModel.Email);
                var msg = MailHelper.CreateSingleTemplateEmail(from, to, templateId, templateDataCliente);

                // ¡Aquí definimos el Asunto!
                msg.Subject = $"Tu pedido #{order.Id} de Corteza Dorada está confirmado";

                await _emailSender.SendEmailWithTemplateAsync(checkoutModel.Email!, templateId!, templateDataCliente);


                // 5b. Correo para el Admin (sigue usando el HTML simple)
                string? adminEmail = _configuration["SendGrid:AdminNotificationEmail"];
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

                    // Necesitamos volver a añadir el método antiguo a la interfaz
                    // O crear una plantilla para el admin...
                    // Solución rápida: Modifiquemos la interfaz de nuevo.

                    // (Veamos el siguiente paso para arreglar esto)
                    await _emailSender.SendEmailAsync(adminEmail, adminSubject, "Nuevo Pedido Recibido", adminHtmlBody);
                }
            }
            catch (Exception ex)
            {
                // Opcional: Registrar el error
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