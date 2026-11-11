using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pedidos360.Models.ViewModels;
using Pedidos360.Security;

namespace Pedidos360.Controllers;

[Authorize(Roles = RoleNames.Admin)]
public class UsersController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;

    public UsersController(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users
            .OrderBy(user => user.Email)
            .ToListAsync();

        var rows = new List<UserListItemViewModel>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            rows.Add(new UserListItemViewModel
            {
                Id = user.Id,
                Email = user.Email ?? user.UserName ?? "(sin correo)",
                Role = roles.FirstOrDefault(role => RoleNames.All.Contains(role)) ?? "Sin rol",
                IsActive = user.LockoutEnd != DateTimeOffset.MaxValue
            });
        }

        return View(rows);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateUserViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new IdentityUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            EmailConfirmed = true,
            LockoutEnabled = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            foreach (var error in roleResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        TempData["Mensaje"] = "Usuario creado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(string id, string role)
    {
        if (!RoleNames.All.Contains(role))
        {
            return BadRequest();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["Error"] = "No puedes cambiar tu propio rol desde esta pantalla.";
            return RedirectToAction(nameof(Index));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        var managedRoles = currentRoles.Where(current => RoleNames.All.Contains(current)).ToArray();
        if (managedRoles.Contains(RoleNames.Admin) && role != RoleNames.Admin &&
            await CountActiveAdminsAsync() <= 1)
        {
            TempData["Error"] = "Debe permanecer al menos un administrador activo.";
            return RedirectToAction(nameof(Index));
        }

        if (managedRoles.Length > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, managedRoles);
            if (!removeResult.Succeeded)
            {
                TempData["Error"] = "No se pudo actualizar el rol del usuario.";
                return RedirectToAction(nameof(Index));
            }
        }

        var addResult = await _userManager.AddToRoleAsync(user, role);
        if (addResult.Succeeded)
        {
            await _userManager.UpdateSecurityStampAsync(user);
        }
        TempData[addResult.Succeeded ? "Mensaje" : "Error"] = addResult.Succeeded
            ? "Rol actualizado correctamente."
            : "No se pudo asignar el rol seleccionado.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        if (user.Id == _userManager.GetUserId(User))
        {
            TempData["Error"] = "No puedes desactivar tu propia cuenta.";
            return RedirectToAction(nameof(Index));
        }

        var isActive = user.LockoutEnd != DateTimeOffset.MaxValue;
        var roles = await _userManager.GetRolesAsync(user);
        if (isActive && roles.Contains(RoleNames.Admin) && await CountActiveAdminsAsync() <= 1)
        {
            TempData["Error"] = "Debe permanecer al menos un administrador activo.";
            return RedirectToAction(nameof(Index));
        }

        user.LockoutEnabled = true;
        user.LockoutEnd = isActive ? DateTimeOffset.MaxValue : null;
        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            await _userManager.UpdateSecurityStampAsync(user);
        }
        TempData[result.Succeeded ? "Mensaje" : "Error"] = result.Succeeded
            ? (isActive ? "Usuario desactivado." : "Usuario activado.")
            : "No se pudo cambiar el estado del usuario.";

        return RedirectToAction(nameof(Index));
    }

    private async Task<int> CountActiveAdminsAsync()
    {
        var admins = await _userManager.GetUsersInRoleAsync(RoleNames.Admin);
        return admins.Count(user => user.LockoutEnd != DateTimeOffset.MaxValue);
    }
}
