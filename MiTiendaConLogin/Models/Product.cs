using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiTiendaConLogin.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Display(Name = "Nombre")]
        public string? Name { get; set; }
        [Display(Name = "Precio")]
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; } // Esto guardará la RUTA de la imagen (ej: /images/mi-ssd.jpg)

        // --- AÑADE ESTAS DOS PROPIEDADES ---

        public int? Stock { get; set; } // <-- Para el inventario

        [NotMapped] // <-- No guarda esto en la BD
        public IFormFile? ImageFile { get; set; } // <-- Para el formulario de subida

        [Display(Name = "Categoría")]
        public int? CategoryId { get; set; } // La llave foránea

        public Category? Category { get; set; } // La "propiedad de navegación"
    }
}