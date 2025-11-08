using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MiTiendaConLogin.Data;
using MiTiendaConLogin.Models;

//
// ▼ ▼ ▼ ¡ESTA ERA LA LÍNEA QUE FALTABA! ▼ ▼ ▼
//
namespace MiTiendaConLogin.Controllers
{
    [Authorize(Roles = "Admin,OrderManager")] // ¡Seguridad lista!
    public class OrdersController : Controller
    {
        // ESTE ES NUESTRO "MINI-MODELO" PARA LA VISTA DE EDICIÓN
        public class OrderEditViewModel
        {
            public int Id { get; set; }
            public string? CustomerEmail { get; set; } // Para mostrar a quién pertenece
            public DateTime OrderDate { get; set; }    // Para mostrar cuándo se hizo
            public string? Status { get; set; }          // El estado actual (que vamos a cambiar)
            
            // La lista de opciones for el menú desplegable
            public List<SelectListItem>? StatusList { get; set; } 
        }

        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Orders (Ahora solo pedidos ACTIVOS)
        public async Task<IActionResult> Index()
        {
            // Definimos los estados "activos"
            var activeStuses = new List<string> { "Pendiente", "En Producción", "Listo para Retirar" };

            var activeOrders = await _context.Orders
                .Where(o => o.Status != null && activeStuses.Contains(o.Status))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(activeOrders);
        }

        // GET: Orders/History (NUEVO MÉTODO PARA EL HISTORIAL)
        public async Task<IActionResult> History()
        {
            // Definimos los estados "inactivos"
            var inactiveStatuses = new List<string> { "Completado", "Cancelado" };

            var pastOrders = await _context.Orders
                .Where(o => o.Status != null && inactiveStatuses.Contains(o.Status))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(pastOrders);
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.OrderDetails!)         // El '!' arregla el error CS8620
                    .ThenInclude(d => d.Product)      
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Orders/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CustomerEmail,OrderDate,RequestedDeliveryDate,Notes,Total,Status")] Order order)
        {
            if (ModelState.IsValid)
            {
                _context.Add(order);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(order);
        }

        // GET: Orders/Edit/5 (NUEVA VERSIÓN CON BLOQUEO)
public async Task<IActionResult> Edit(int? id)
{
    if (id == null)
    {
        return NotFound();
    }

    var order = await _context.Orders.FindAsync(id);
    if (order == null)
    {
        return NotFound();
    }

    // --- ¡AQUÍ ESTÁ LA LÓGICA DE TU IDEA! ---
    // Si el pedido está "Completado" O "Cancelado"...
    if (order.Status == "Completado" || order.Status == "Cancelado")
    {
        // ...Y el usuario actual NO es un Admin...
        if (!User.IsInRole("Admin"))
        {
            // ...¡Bloquéalo!
            TempData["ErrorMessage"] = $"El pedido #{order.Id} ya está '{order.Status}' y solo un Administrador puede modificarlo.";
            return RedirectToAction(nameof(Index));
        }
    }
    // --- FIN DEL BLOQUEO ---

    // Si eres Admin (o el pedido no está completado), puedes continuar.
    var statusOptions = new List<string> 
    { 
        "Pendiente", 
        "En Producción", 
        "Listo para Retirar", 
        "Completado", 
        "Cancelado" 
    };

    var viewModel = new OrderEditViewModel
    {
        Id = order.Id,
        CustomerEmail = order.CustomerEmail,
        OrderDate = order.OrderDate,
        Status = order.Status,
        StatusList = statusOptions.Select(s => new SelectListItem
        {
            Text = s,
            Value = s,
            Selected = (s == order.Status)
        }).ToList()
    };

    return View(viewModel);
}

        // POST: Orders/Edit/5 (¡VERSIÓN FINAL CON DEDUCCIÓN Y REVERSIÓN!)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, OrderEditViewModel viewModel)
    {
        if (id != viewModel.Id)
        {
            return NotFound();
        }

        // Buscamos la orden original, INCLUYENDO los productos
        var orderToUpdate = await _context.Orders
            .Include(o => o.OrderDetails!)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (orderToUpdate == null)
        {
            return NotFound();
        }

        // --- DOBLE CHEQUEO DE SEGURIDAD (para el empleado "travieso") ---
        if (orderToUpdate.Status == "Completado" || orderToUpdate.Status == "Cancelado")
        {
            if (!User.IsInRole("Admin"))
            {
                // Si un OrderManager envía un POST manual, lo bloqueamos.
                TempData["ErrorMessage"] = "Acción no autorizada.";
                return RedirectToAction(nameof(Index));
            }
        }
        // --- FIN DEL DOBLE CHEQUEO ---


        // --- LÓGICA DE INVENTARIO (CON REVERSIÓN DE ADMIN) ---

        // Caso 1: Se está marcando como "Completado" (y antes no lo era)
        // ¡RESTAMOS STOCK!
        if (viewModel.Status == "Completado" && orderToUpdate.Status != "Completado")
        {
            foreach (var detail in orderToUpdate.OrderDetails!)
            {
                if (detail.Product != null && detail.Product.Stock != null)
                {
                    detail.Product.Stock -= detail.Quantity;
                }
            }
        }
        
        // Caso 2: Estaba "Completado" y el Admin lo está revirtiendo
        // ¡SUMAMOS STOCK DE VUELTA!
        else if (viewModel.Status != "Completado" && orderToUpdate.Status == "Completado")
        {
            foreach (var detail in orderToUpdate.OrderDetails!)
            {
                if (detail.Product != null && detail.Product.Stock != null)
                {
                    detail.Product.Stock += detail.Quantity; // <-- ¡Usamos += para sumar!
                }
            }
        }
        
        // --- FIN DE LÓGICA DE INVENTARIO ---

        // Actualizamos el estado de la orden
        orderToUpdate.Status = viewModel.Status;

        try
        {
            // Guardamos todos los cambios (el estado de la Orden Y el stock de los Productos)
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!OrderExists(orderToUpdate.Id))
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

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}