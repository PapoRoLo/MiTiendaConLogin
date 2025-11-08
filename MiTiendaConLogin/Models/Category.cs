using System.ComponentModel.DataAnnotations;

namespace MiTiendaConLogin.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Nombre de la Categoría")]
        public string? Name { get; set; }

        // Esto permite que una categoría tenga muchos productos
        public List<Product>? Products { get; set; }
    }
}