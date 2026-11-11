using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Pedidos360.Data;
using Pedidos360.Models;
using Pedidos360.Security;

namespace Pedidos360.Controllers;

[Authorize(Roles = RoleNames.AllRoles)]
public class ProductosController : Controller
{
    private readonly ApplicationDbContext _context;
   

    public ProductosController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(
        string? buscar,
        int? categoriaId,
        string? estado)
    {
        var query = _context.Productos
            .Include(p => p.Categoria)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(buscar))
            query = query.Where(p => p.Nombre.Contains(buscar));

        if (categoriaId.HasValue && categoriaId > 0)
            query = query.Where(p => p.CategoriaId == categoriaId);

        if (estado == "activos")
            query = query.Where(p => p.Activo);
        else if (estado == "inactivos")
            query = query.Where(p => !p.Activo);

        // La paginación la hace la vista con JavaScript, por eso se envía la lista completa filtrada
        var productos = await query.OrderBy(p => p.Nombre).ToListAsync();

        // Totales de las tarjetas (de todo el catálogo, sin filtros)
        ViewData["TotalProductos"] = await _context.Productos.CountAsync();
        ViewData["TotalCategorias"] = await _context.Categorias.CountAsync();
        ViewData["TotalActivos"] = await _context.Productos.CountAsync(p => p.Activo);
        ViewData["TotalInactivos"] = await _context.Productos.CountAsync(p => !p.Activo);

        ViewData["BuscarActual"] = buscar;
        ViewData["EstadoActual"] = estado;
        ViewData["CategoriaActual"] = categoriaId;
        ViewData["Categorias"] = new SelectList(
            await _context.Categorias.OrderBy(c => c.Nombre).ToListAsync(),
            "Id",
            "Nombre",
            categoriaId
        );

        return View(productos);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var producto = await _context.Productos
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Id == id);

        return producto == null ? NotFound() : View(producto);
    }

    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Create()
    {
        await CargarCategorias();
        return View();
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Producto producto, IFormFile? imagenFile)
    {
        // La imagen se recibe desde imagenFile
        ModelState.Remove(nameof(Producto.ImagenUrl));

        if (imagenFile == null || imagenFile.Length == 0)
        {
            ModelState.AddModelError(nameof(Producto.ImagenUrl),
                "La imagen del producto es obligatoria.");
        }

        if (!ModelState.IsValid)
        {
            await CargarCategorias(producto.CategoriaId);
            return View(producto);
        }

        producto.ImagenUrl = await GuardarImagen(imagenFile!);

        _context.Productos.Add(producto);
        await _context.SaveChangesAsync();

        TempData["Mensaje"] = "Producto creado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var producto = await _context.Productos.FindAsync(id);
        if (producto == null) return NotFound();

        await CargarCategorias(producto.CategoriaId);
        return View(producto);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Producto producto, IFormFile? imagenFile)
    {
        if (id != producto.Id) return NotFound();

        ModelState.Remove(nameof(Producto.ImagenUrl));

        if (!ModelState.IsValid)
        {
            await CargarCategorias(producto.CategoriaId);
            return View(producto);
        }

        var productoDb = await _context.Productos.FindAsync(id);
        if (productoDb == null) return NotFound();

        productoDb.Nombre = producto.Nombre;
        productoDb.CategoriaId = producto.CategoriaId;
        productoDb.Precio = producto.Precio;
        productoDb.ImpuestoPorc = producto.ImpuestoPorc;
        productoDb.Stock = producto.Stock;
        productoDb.Activo = producto.Activo;

        if (imagenFile != null && imagenFile.Length > 0)
        {
            var imagenAnterior = productoDb.ImagenUrl;
            productoDb.ImagenUrl = await GuardarImagen(imagenFile);

            EliminarImagen(imagenAnterior);
        }

        await _context.SaveChangesAsync();

        TempData["Mensaje"] = "Producto actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.Admin)]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var producto = await _context.Productos
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Id == id);

        return producto == null ? NotFound() : View(producto);
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = RoleNames.Admin)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var producto = await _context.Productos.FindAsync(id);

        if (producto != null)
        {
            producto.Activo = false;
            await _context.SaveChangesAsync();
        }

        TempData["Mensaje"] = "Producto desactivado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private async Task CargarCategorias(int? categoriaSeleccionada = null)
    {
        var categorias = await _context.Categorias
            .OrderBy(c => c.Nombre)
            .ToListAsync();

        ViewData["Categorias"] = new SelectList(
            categorias,
            "Id",
            "Nombre",
            categoriaSeleccionada
        );
    }

    private async Task<string> GuardarImagen(IFormFile imagenFile)
    {
        var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(imagenFile.FileName).ToLowerInvariant();

        if (!extensionesPermitidas.Contains(extension))
        {
            throw new InvalidOperationException(
                "La imagen debe ser JPG, JPEG, PNG o WEBP.");
        }

        var carpeta = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "images",
            "productos"
        );

        Directory.CreateDirectory(carpeta);

        var nombreArchivo = $"{Guid.NewGuid():N}{extension}";
        var rutaCompleta = Path.Combine(carpeta, nombreArchivo);

        await using var stream = new FileStream(rutaCompleta, FileMode.Create);
        await imagenFile.CopyToAsync(stream);

        return $"/images/productos/{nombreArchivo}";
    }

    private void EliminarImagen(string? imagenUrl)
    {
        if (string.IsNullOrWhiteSpace(imagenUrl)) return;

        var rutaRelativa = imagenUrl
            .TrimStart('/')
            .Replace('/', Path.DirectorySeparatorChar);

        var rutaCompleta = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            rutaRelativa
        );

        if (System.IO.File.Exists(rutaCompleta))
        {
            System.IO.File.Delete(rutaCompleta);
        }
    }
}
