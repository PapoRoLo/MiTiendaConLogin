using System.ComponentModel.DataAnnotations;

namespace MiTiendaConLogin.Models
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Tu nombre es requerido")]
        [Display(Name = "Nombre Completo")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Tu email es requerido")]
        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Tu teléfono es requerido")]
        [Phone]
        [Display(Name = "Teléfono de Contacto")]
        public string? Phone { get; set; }

        [Display(Name = "Fecha de Entrega/Retiro")]
        [DataType(DataType.Date)]
        // Hacemos que la fecha de entrega sea requerida
        [Required(ErrorMessage = "Por favor, dinos cuándo lo necesitas")]
        public DateTime RequestedDeliveryDate { get; set; } = DateTime.Today.AddDays(1); // Valor por defecto: mañana

        [Display(Name = "Notas Adicionales (Ej. 'Pastel de chocolate, que diga Feliz Cumpleaños')")]
        [DataType(DataType.MultilineText)]
        public string? Notes { get; set; }
    }
}