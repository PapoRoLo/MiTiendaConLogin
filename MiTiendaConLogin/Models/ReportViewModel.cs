using System;
using System.Collections.Generic;

namespace MiTiendaConLogin.Models
{
    // Sub-modelo para la lista de productos más vendidos (reutilizable)
    public class TopSellingProduct
    {
        public string? ProductName { get; set; }
        public int TotalSold { get; set; }
    }

    // Sub-modelo para las tarjetas de resumen por defecto
    public class ReportSummary
    {
        public decimal SalesToday { get; set; }
        public decimal SalesThisWeek { get; set; }
        public decimal SalesThisMonth { get; set; }
        public int TotalOrdersThisMonth { get; set; }
    }

    // Sub-modelo para el resultado de la consulta personalizada
    public class CustomReportResult
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalSales { get; set; }
        public int TotalOrders { get; set; }
        public List<TopSellingProduct> TopProducts { get; set; } = new List<TopSellingProduct>();
    }

    // El Modelo Principal que se envía a la página
    public class ReportViewModel
    {
        // Siempre mostraremos el resumen por defecto
        public ReportSummary DefaultSummary { get; set; } = new ReportSummary();

        // Esta parte solo se llenará si el usuario hace una consulta
        public CustomReportResult? CustomResult { get; set; } 
    }
}