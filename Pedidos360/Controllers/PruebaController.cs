
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Pedidos360.Controllers
{
    [AllowAnonymous]
    public class PruebaController : Controller
    {
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IWebHostEnvironment _environment;

        public PruebaController(
            SignInManager<IdentityUser> signInManager,
            IWebHostEnvironment environment)
        {
            _signInManager = signInManager;
            _environment = environment;
        }

        public async Task<IActionResult> IniciarSesion()
        {
            if (!_environment.IsDevelopment())
                return NotFound();

            var resultado = await _signInManager.PasswordSignInAsync(
                "prueba@pedidos360.local",
                "Prueba123!",
                false,
                false
            );

            if (!resultado.Succeeded)
                return BadRequest("No se pudo iniciar sesión con el usuario de prueba.");

            return RedirectToAction("Index", "Pedidos");
        }
    }
}
