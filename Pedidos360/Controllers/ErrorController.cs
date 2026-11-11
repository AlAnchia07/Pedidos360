using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Pedidos360.Controllers
{
    [AllowAnonymous]
    public class ErrorController : Controller
    {
        [Route("Error/{codigo:int}")]
        public IActionResult Index(int codigo)
        {
            if (codigo == 404)
            {
                return View("NoEncontrado");
            }

            return View("General");
        }
    }
}