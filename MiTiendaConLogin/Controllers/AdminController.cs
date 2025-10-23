using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MiTiendaConLogin.Controllers
{
    [Authorize(Roles = "Admin")] // ¡SOLO ADMINS PUEDEN ENTRAR AQUÍ!
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        // Muestra una lista de todos los usuarios
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        // --------- AQUÍ ESTÁ LA LÓGICA DE RESETEO ---------

        // Muestra el formulario para resetear la contraseña
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Pasamos el Email y el ID a la vista
            var model = new { UserId = user.Id, Email = user.Email };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string userId, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // 1. Generamos un token de reseteo (como el que se envía por email)
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // 2. Usamos ese token para forzar el cambio
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                // ¡Éxito!
                return RedirectToAction("Index");
            }

            // Si algo falló, muestra los errores
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            // (Necesitarás crear una vista simple para esto)
            return View(new { UserId = user.Id, Email = user.Email });
        }
    }
}