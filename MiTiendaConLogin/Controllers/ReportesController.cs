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

        // El método ahora acepta fechas opcionales
        public async Task<IActionResult> Index(DateTime? customDate, DateTime? startDate, DateTime? endDate)
        {
            var viewModel = new ReportViewModel();

            // --- 1. CALCULAR SIEMPRE EL RESUMEN POR DEFECTO (TARJETAS) ---
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            
            // Base del resumen: pedidos completados este mes
            var ordersForSummary = await _context.Orders
                .Where(o => o.Status == "Completado" && o.OrderDate >= startOfMonth)
                .ToListAsync();

            viewModel.DefaultSummary.SalesToday = ordersForSummary
                .Where(o => o.OrderDate >= today)
                .Sum(o => o.Total);
            viewModel.DefaultSummary.SalesThisWeek = ordersForSummary
                .Where(o => o.OrderDate >= startOfWeek)
                .Sum(o => o.Total);
            viewModel.DefaultSummary.SalesThisMonth = ordersForSummary
                .Sum(o => o.Total);
            viewModel.DefaultSummary.TotalOrdersThisMonth = ordersForSummary.Count;

            
            // --- 2. PROCESAR LA CONSULTA PERSONALIZADA (SI EXISTE) ---
            DateTime queryStartDate;
            DateTime queryEndDate;

            if (customDate.HasValue)
            {
                // Opción A: Reporte de un solo día
                queryStartDate = customDate.Value.Date;
                queryEndDate = customDate.Value.Date.AddDays(1).AddTicks(-1); // El día completo
            }
            else if (startDate.HasValue && endDate.HasValue)
            {
                // Opción B: Reporte por rango
                queryStartDate = startDate.Value.Date;
                queryEndDate = endDate.Value.Date.AddDays(1).AddTicks(-1); // El día completo
            }
            else
            {
                // Opción C: Sin consulta, solo mostrar la vista
                return View(viewModel);
            }

            // --- 3. EJECUTAR LA CONSULTA PERSONALIZADA ---

            // Base de la consulta: pedidos completados en el rango
            var ordersForCustomQuery = await _context.Orders
                .Where(o => o.Status == "Completado" && o.OrderDate >= queryStartDate && o.OrderDate <= queryEndDate)
                .Include(o => o.OrderDetails!)
                    .ThenInclude(d => d.Product)
                .ToListAsync();
            
            // Llenar el objeto de resultado
            viewModel.CustomResult = new CustomReportResult
            {
                StartDate = queryStartDate,
                EndDate = queryEndDate,
                TotalSales = ordersForCustomQuery.Sum(o => o.Total),
                TotalOrders = ordersForCustomQuery.Count,
                TopProducts = ordersForCustomQuery
                    .SelectMany(o => o.OrderDetails!)
                    .GroupBy(d => d.Product?.Name)
                    .Select(g => new TopSellingProduct
                    {
                        ProductName = g.Key ?? "Producto Desconocido",
                        TotalSold = g.Sum(d => d.Quantity)
                    })
                    .OrderByDescending(p => p.TotalSold)
                    .Take(10)
                    .ToList()
            };

            return View(viewModel);
        }
    }
}