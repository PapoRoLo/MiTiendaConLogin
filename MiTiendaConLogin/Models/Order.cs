using System.ComponentModel.DataAnnotations;

namespace MiTiendaConLogin.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Display(Name = "Email del Cliente")]
        public string? CustomerEmail { get; set; }

        [Display(Name = "Fecha de Orden")]
        public DateTime OrderDate { get; set; }

        [Display(Name = "Total")]
        public decimal Total { get; set; }

        [Display(Name = "Estado")]
        public string? Status { get; set; } // Ej: "Pendiente", "Enviado", "Cancelado"

        // Nota: Esto es un modelo simple. 
        // Más adelante, podríamos añadir una lista de "OrderDetails" 
        // para saber QUÉ productos compró.
    }
}