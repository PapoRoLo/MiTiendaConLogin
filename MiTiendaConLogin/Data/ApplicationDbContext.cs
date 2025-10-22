using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiTiendaConLogin.Models;

namespace MiTiendaConLogin.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // AÑADE ESTA LÍNEA:
    public DbSet<Product> Products { get; set; }
}
