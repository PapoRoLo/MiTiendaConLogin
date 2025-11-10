using Microsoft.AspNetCore.Identity;
using MiTiendaConLogin.Models; // Para ApplicationUser

namespace MiTiendaConLogin.Data
{
    // Esta clase se encargará de crear Roles y asignar el Admin
    public class RoleSeeder
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public RoleSeeder(RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        // Método 1: Crea los 3 roles si no existen
        public async Task SeedRolesAsync()
        {
            string[] roleNames = { "Admin", "ProductManager", "OrderManager" };

            foreach (var roleName in roleNames)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
            
            // ¡Llama al método 2 para asignar el admin!
            await SeedAdminUserAsync();
        }

        // Método 2: Busca tu email y lo hace Admin
        private async Task SeedAdminUserAsync()
        {

            return;

            // ▼▼▼ ¡IMPORTANTE! ▼▼▼
            // Reemplaza esto con el email que acabas de registrar
            //string adminEmail = "bran506rolo@gmail.com"; 

            // 1. Buscar al usuario
            //var adminUser = await _userManager.FindByEmailAsync(adminEmail);

            //if (adminUser == null)
            //{
                // Si el usuario no existe, no hacemos nada
                //return;
            //}

            // 2. Revisar si ya es Admin
            //if (!await _userManager.IsInRoleAsync(adminUser, "Admin"))
            //{
                // 3. Si no es Admin, añadirlo al rol
                //await _userManager.AddToRoleAsync(adminUser, "Admin");
            //}
        }
    }
}