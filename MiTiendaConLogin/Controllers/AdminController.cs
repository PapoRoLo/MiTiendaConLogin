using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiTiendaConLogin.Models;

namespace MiTiendaConLogin.Controllers
{
    [Authorize(Roles = "Admin")] // ¡SOLO ADMINS PUEDEN ENTRAR AQUÍ!
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
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

        // --- INICIO DE ACCIONES PARA GESTIONAR ROLES ---

        // GET: /Admin/ManageUserRoles/id-del-usuario
        public async Task<IActionResult> ManageUserRoles(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new ManageUserRolesViewModel
            {
                UserId = user.Id,
                Email = user.Email,
                Roles = new List<RoleViewModel>()
            };

            // Llenar la lista de checkboxes
            foreach (var role in await _roleManager.Roles.ToListAsync())
            {
                viewModel.Roles.Add(new RoleViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name,
                    // Revisa si el usuario ya tiene este rol
                    IsSelected = await _userManager.IsInRoleAsync(user, role.Name)
                });
            }

            return View(viewModel);
        }

        // POST: /Admin/ManageUserRoles
        [HttpPost]
        public async Task<IActionResult> ManageUserRoles(ManageUserRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            // Obtener los roles que el usuario SÍ tenía
            var userRoles = await _userManager.GetRolesAsync(user);

            // Recorrer los checkboxes que se enviaron
            foreach (var role in model.Roles)
            {
                // Si está seleccionado y el usuario NO lo tiene -> Añadirlo
                if (role.IsSelected && !userRoles.Contains(role.RoleName))
                {
                    await _userManager.AddToRoleAsync(user, role.RoleName);
                }
                // Si NO está seleccionado y el usuario SÍ lo tiene -> Quitarlo
                else if (!role.IsSelected && userRoles.Contains(role.RoleName))
                {
                    await _userManager.RemoveFromRoleAsync(user, role.RoleName);
                }
            }

            return RedirectToAction("Index");
        }
        // --- FIN DE ACCIONES ---
    }
}