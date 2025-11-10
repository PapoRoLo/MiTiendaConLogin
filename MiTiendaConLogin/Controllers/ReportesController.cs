using Microsoft.AspNetCore.Mvc;
using MiTiendaConLogin.Data;
using MiTiendaConLogin.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;

namespace MiTiendaConLogin.Controllers
{
    [Authorize(Roles = "Admin,OrderManager")]
    public class ReportesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // 1. Lógica para las tarjetas de resumen por defecto
            var today = DateTime.UtcNow.Date;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            var startOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            // 2. Obtener todas las órdenes completadas
            var allCompletedOrders = await _context.Orders
                .Where(o => o.Status == "Completado")
                .ToListAsync();

            // 3. Calcular los resúmenes
            var defaultSummary = new ReportSummary
            {
                SalesToday = allCompletedOrders.Where(o => o.OrderDate >= today).Sum(o => o.Total),
                SalesThisWeek = allCompletedOrders.Where(o => o.OrderDate >= startOfWeek).Sum(o => o.Total),
                SalesThisMonth = allCompletedOrders.Where(o => o.OrderDate >= startOfMonth).Sum(o => o.Total),
                TotalOrdersThisMonth = allCompletedOrders.Count(o => o.OrderDate >= startOfMonth)
            };

            // 4. Crear el ViewModel
            var viewModel = new ReportViewModel
            {
                DefaultSummary = defaultSummary,
                CustomResult = null, // No hay resultado personalizado al inicio
                IndividualOrders = new List<Order>() // No hay órdenes individuales al inicio
            };

            // 5. Devolver la vista
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(DateTime? customDate, DateTime? startDate, DateTime? endDate)
        {
            // --- 1. Obtener los resúmenes por defecto (DE NUEVO) ---
            //    (Necesitamos la lógica de tu método [HttpGet] aquí para
            //    que las tarjetas de "Hoy", "Semana", "Mes" no desaparezcan)

            // Voy a asumir la lógica de tu método GET, si es diferente, ajústala.
            var today = DateTime.UtcNow.Date;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            var startOfMonth = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var allCompletedOrders = await _context.Orders
                .Where(o => o.Status == "Completado")
                .ToListAsync();

            var defaultSummary = new ReportSummary
            {
                SalesToday = allCompletedOrders.Where(o => o.OrderDate >= today).Sum(o => o.Total),
                SalesThisWeek = allCompletedOrders.Where(o => o.OrderDate >= startOfWeek).Sum(o => o.Total),
                SalesThisMonth = allCompletedOrders.Where(o => o.OrderDate >= startOfMonth).Sum(o => o.Total),
                TotalOrdersThisMonth = allCompletedOrders.Count(o => o.OrderDate >= startOfMonth)
            };
            // --- Fin de la lógica GET ---


            // --- 2. Lógica de la Consulta Personalizada (la que ya teníamos) ---
            IQueryable<Order> ordersQuery = _context.Orders
                .Where(o => o.Status == "Completado")
                .Include(o => o.OrderDetails!)
                .ThenInclude(d => d.Product);

            DateTime queryStartDate = DateTime.UtcNow.Date;
            DateTime queryEndDate = DateTime.UtcNow.Date;

            if (customDate.HasValue)
            {
                var localDate = customDate.Value.Date;
                var utcDate = DateTime.SpecifyKind(localDate, DateTimeKind.Utc);
                var utcEnd = utcDate.AddDays(1);

                ordersQuery = ordersQuery.Where(o => o.OrderDate >= utcDate && o.OrderDate < utcEnd);
                queryStartDate = utcDate;
                queryEndDate = utcDate;
            }
            else if (startDate.HasValue && endDate.HasValue)
            {
                var localStart = startDate.Value.Date;
                var localEnd = endDate.Value.Date;

                var utcStart = DateTime.SpecifyKind(localStart, DateTimeKind.Utc);
                var utcEnd = DateTime.SpecifyKind(localEnd, DateTimeKind.Utc).AddDays(1);

                ordersQuery = ordersQuery.Where(o => o.OrderDate >= utcStart && o.OrderDate < utcEnd);
                queryStartDate = utcStart;
                queryEndDate = localEnd; // Usamos la fecha local para mostrarla
            }
            else
            {
                // No hacer nada o devolver error
                return RedirectToAction("Index");
            }

            // --- 3. Ejecutar la consulta y llenar el ViewModel ---

            // Ejecutamos la consulta para la lista individual
            var individualOrdersList = await ordersQuery.OrderByDescending(o => o.OrderDate).ToListAsync();

            // Calculamos los resúmenes para el CustomResult
            var topProducts = individualOrdersList
                .SelectMany(o => o.OrderDetails!)
                .GroupBy(d => d.Product?.Name ?? "Producto Desconocido")
                .Select(g => new TopSellingProduct
                {
                    ProductName = g.Key,
                    TotalSold = g.Sum(d => d.Quantity)
                })
                .OrderByDescending(p => p.TotalSold)
                .ToList();

            var customResult = new CustomReportResult
            {
                StartDate = queryStartDate,
                EndDate = queryEndDate,
                TotalSales = individualOrdersList.Sum(o => o.Total),
                TotalOrders = individualOrdersList.Count,
                TopProducts = topProducts
            };

            // --- 4. Construir el ViewModel FINAL ---
            var viewModel = new ReportViewModel
            {
                DefaultSummary = defaultSummary, // Las tarjetas de arriba
                CustomResult = customResult,     // Los resúmenes de la consulta
                IndividualOrders = individualOrdersList // ¡La lista de pedidos!
            };

            // 5. Devolver la vista con el MODELO COMPLETO
            return View("Index", viewModel);
        }
    }
}