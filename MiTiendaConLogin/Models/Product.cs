using System.ComponentModel.DataAnnotations.Schema;

namespace MiTiendaConLogin.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; } // Esto guardará la RUTA de la imagen (ej: /images/mi-ssd.jpg)

        // --- AÑADE ESTAS DOS PROPIEDADES ---

        public int Stock { get; set; } // <-- Para el inventario

        [NotMapped] // <-- No guarda esto en la BD
        public IFormFile? ImageFile { get; set; } // <-- Para el formulario de subida
    }
}