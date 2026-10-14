using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pedidos360.Data;

namespace Pedidos360.Controllers.Api
{
    [Route("api/productos")]
    [ApiController]
    [Authorize] // Requerir autenticación como mínimo
    public class ProductosApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductosApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /api/productos/buscar?q=...&limit=10
        [HttpGet("buscar")]
        public async Task<IActionResult> Buscar([FromQuery] string q, [FromQuery] int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(q)) return Ok(new object[0]);

            limit = Math.Min(Math.Max(limit, 1), 50);

            var items = await _context.Productos
                .Where(p => p.Nombre.Contains(q))
                .OrderBy(p => p.Nombre)
                .Take(limit)
                .Select(p => new
                {
                    id = p.Id,
                    nombre = p.Nombre,
                    precio = p.Precio,
                    impuesto = p.ImpuestoPorc,
                    stock = p.Stock
                })
                .ToListAsync();

            return Ok(items);
        }
    }
}
