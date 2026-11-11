using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pedidos360.Data;
using Pedidos360.Models;

namespace Pedidos360.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private const int UmbralStockBajo = 10;

        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new DashboardViewModel
            {
                TotalProductos = await _context.Productos.CountAsync(p => p.Activo),
                TotalClientes = await _context.Clientes.CountAsync(),
                TotalPedidos = await _context.Pedidos.CountAsync(),
                StockTotal = await _context.Productos.SumAsync(p => (int?)p.Stock) ?? 0,
                BajoStock = await _context.Productos
                    .Where(p => p.Stock <= UmbralStockBajo)
                    .OrderBy(p => p.Stock)
                    .Take(8)
                    .Select(p => new ProductoBajoStock
                    {
                        ImagenUrl = p.ImagenUrl,
                        Nombre = p.Nombre,
                        Categoria = _context.Categorias
                            .Where(c => c.Id == p.CategoriaId)
                            .Select(c => c.Nombre).FirstOrDefault() ?? "—",
                        Stock = p.Stock,
                        Activo = p.Activo
                    })
                    .ToListAsync()
            };

            return View(vm);
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}