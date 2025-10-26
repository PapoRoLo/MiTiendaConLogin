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
        // GET: /Admin/ResetPassword/id-del-usuario
        public async Task<IActionResult> ResetPassword(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Creamos un objeto simple para pasar los datos a la vista
            var model = new { UserId = user.Id, Email = user.Email };
            return View(model);
        }

        // POST: /Admin/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string userId, string newPassword)
        {
            if (userId == null || newPassword == null)
            {
                return RedirectToAction("Index"); // Error simple
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // --- INICIO DE LA LÓGICA DE ADMIN CORRECTA ---

            // 1. Eliminamos cualquier contraseña que el usuario tenga.
            //    Esto es robusto, funciona incluso si el usuario se registró
            //    con Google y no tenía una contraseña local.
            var removeResult = await _userManager.RemovePasswordAsync(user);

            if (!removeResult.Succeeded)
            {
                // Si no se pudo quitar (raro), aun así intentamos añadir.
                // Pero por si acaso, registramos los errores.
                foreach (var error in removeResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // 2. Añadimos la nueva contraseña que el Admin escribió.
            var addResult = await _userManager.AddPasswordAsync(user, newPassword);

            // --- FIN DE LA LÓGICA ---

            if (addResult.Succeeded)
            {
                TempData["SuccessMessage"] = $"¡La contraseña para {user.Email} se ha reseteado exitosamente!";
                // ¡Éxito! Volvemos al panel de admin
                return RedirectToAction("Index");
            }

            // Si falló (ej. la contraseña es muy débil), volvemos a la página
            // con los errores para que el admin sepa qué pasó.
            foreach (var error in addResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            // Volvemos a enviar los datos necesarios a la vista
            var model = new { UserId = user.Id, Email = user.Email };
            return View(model);
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