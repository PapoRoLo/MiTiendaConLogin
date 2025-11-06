using Microsoft.AspNetCore.Identity;

namespace MiTiendaConLogin.Models
{
    // Esta clase hereda todas las propiedades de IdentityUser
    // (como Email, PasswordHash, etc.) y le añade las nuestras.
    public class ApplicationUser : IdentityUser
    {
        public string? Nombre { get; set; }
    }
}