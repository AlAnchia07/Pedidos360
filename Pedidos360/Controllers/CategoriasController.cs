using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pedidos360.Data;
using Pedidos360.Models;
using Pedidos360.Security;

namespace Pedidos360.Controllers
{
    [Authorize(Roles = RoleNames.AllRoles)]
    public class CategoriasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CategoriasController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var categorias = await _context.Categorias
                .Include(c => c.Productos)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            return View(categorias);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var categoria = await _context.Categorias
                .Include(c => c.Productos)
                .FirstOrDefaultAsync(c => c.Id == id);

            return categoria == null ? NotFound() : View(categoria);
        }

        [Authorize(Roles = RoleNames.Admin)]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Categoria categoria)
        {
            var nombre = categoria.Nombre?.Trim() ?? "";

            if (await _context.Categorias.AnyAsync(c => c.Nombre.ToLower() == nombre.ToLower()))
                ModelState.AddModelError(nameof(categoria.Nombre), "Ya existe una categoría con ese nombre.");

            if (!ModelState.IsValid)
                return View(categoria);

            categoria.Nombre = nombre;

            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Categoría creada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var categoria = await _context.Categorias.FindAsync(id);

            return categoria == null ? NotFound() : View(categoria);
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Categoria categoria)
        {
            if (id != categoria.Id) return NotFound();

            var nombre = categoria.Nombre?.Trim() ?? "";

            if (await _context.Categorias.AnyAsync(c =>
                c.Id != id && c.Nombre.ToLower() == nombre.ToLower()))
                ModelState.AddModelError(nameof(categoria.Nombre), "Ya existe otra categoría con ese nombre.");

            if (!ModelState.IsValid)
                return View(categoria);

            var categoriaDb = await _context.Categorias.FindAsync(id);
            if (categoriaDb == null) return NotFound();

            categoriaDb.Nombre = nombre;

            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Categoría actualizada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var categoria = await _context.Categorias
                .Include(c => c.Productos)
                .FirstOrDefaultAsync(c => c.Id == id);

            return categoria == null ? NotFound() : View(categoria);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = RoleNames.Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var categoria = await _context.Categorias
                .Include(c => c.Productos)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (categoria == null) return NotFound();

            if (categoria.Productos.Any())
            {
                TempData["Error"] = "No se puede eliminar esta categoría porque tiene productos asociados.";
                return RedirectToAction(nameof(Index));
            }

            _context.Categorias.Remove(categoria);
            await _context.SaveChangesAsync();

            TempData["Mensaje"] = "Categoría eliminada correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}
