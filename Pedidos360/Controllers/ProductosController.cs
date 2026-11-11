using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Pedidos360.Data;
using Pedidos360.Models;
using Pedidos360.Security;
using System.ComponentModel.DataAnnotations;

namespace Pedidos360.Controllers;

[Authorize(Roles = RoleNames.AllRoles)]
public class ProductosController : Controller
{
    private readonly ApplicationDbContext _context;

    private const long MaxImagenBytes = 5 * 1024 * 1024;

    public ProductosController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? buscar, int? categoriaId, string? estado)
    {
        var query = _context.Productos.Include(p => p.Categoria).AsQueryable();

        if (!string.IsNullOrWhiteSpace(buscar))
            query = query.Where(p => p.Nombre.Contains(buscar));

        if (categoriaId.HasValue && categoriaId > 0)
            query = query.Where(p => p.CategoriaId == categoriaId);

        if (estado == "activos")
            query = query.Where(p => p.Activo);
        else if (estado == "inactivos")
            query = query.Where(p => !p.Activo);

        var productos = await query.OrderBy(p => p.Nombre).ToListAsync();

        ViewData["TotalProductos"] = await _context.Productos.CountAsync();
        ViewData["TotalCategorias"] = await _context.Categorias.CountAsync();
        ViewData["TotalActivos"] = await _context.Productos.CountAsync(p => p.Activo);
        ViewData["TotalInactivos"] = await _context.Productos.CountAsync(p => !p.Activo);

        ViewData["BuscarActual"] = buscar;
        ViewData["EstadoActual"] = estado;
        ViewData["CategoriaActual"] = categoriaId;

        ViewData["Categorias"] = new SelectList(
            await _context.Categorias.OrderBy(c => c.Nombre).ToListAsync(),
            "Id", "Nombre", categoriaId);

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

    // Mostrar imagen almacenada en la base de datos
    [HttpGet]
    public async Task<IActionResult> Imagen(int id)
    {
        var producto = await _context.Productos
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (producto == null) return NotFound();

        if (producto.ImagenDatos != null && producto.ImagenDatos.Length > 0)
        {
            return File(
                producto.ImagenDatos,
                producto.ImagenContentType ?? "application/octet-stream");
        }

        if (!string.IsNullOrWhiteSpace(producto.ImagenUrl))
            return Redirect(producto.ImagenUrl);

        return NotFound();
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
        ModelState.Remove(nameof(Producto.ImagenUrl));
        ModelState.Remove(nameof(Producto.ImagenDatos));
        ModelState.Remove(nameof(Producto.ImagenContentType));

        if (!await _context.Categorias.AnyAsync(c => c.Id == producto.CategoriaId))
            ModelState.AddModelError(nameof(producto.CategoriaId), "Seleccione una categoría válida.");

        if (imagenFile == null || imagenFile.Length == 0)
            ModelState.AddModelError("imagenFile", "Debe seleccionar una imagen.");
        else if (!ImagenValida(imagenFile))
            ModelState.AddModelError("imagenFile", "Seleccione una imagen JPG, JPEG, PNG o WEBP de máximo 5 MB.");

        if (!ModelState.IsValid)
        {
            await CargarCategorias(producto.CategoriaId);
            return View(producto);
        }

        producto.ImagenDatos = await LeerImagen(imagenFile!);
        producto.ImagenContentType = ObtenerContentType(imagenFile!);
        producto.ImagenUrl = string.Empty;

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
        ModelState.Remove(nameof(Producto.ImagenDatos));
        ModelState.Remove(nameof(Producto.ImagenContentType));

        if (!await _context.Categorias.AnyAsync(c => c.Id == producto.CategoriaId))
            ModelState.AddModelError(nameof(producto.CategoriaId), "Seleccione una categoría válida.");

        if (imagenFile != null && imagenFile.Length > 0 && !ImagenValida(imagenFile))
            ModelState.AddModelError("imagenFile", "Seleccione una imagen JPG, JPEG, PNG o WEBP de máximo 5 MB.");

        if (!ModelState.IsValid)
        {
            var imagenActual = await _context.Productos
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (imagenActual == null) return NotFound();

            producto.ImagenDatos = imagenActual.ImagenDatos;
            producto.ImagenContentType = imagenActual.ImagenContentType;

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
        productoDb.StockMinimo = producto.StockMinimo;
        productoDb.Activo = producto.Activo;

        if (imagenFile != null && imagenFile.Length > 0)
        {
            productoDb.ImagenDatos = await LeerImagen(imagenFile);
            productoDb.ImagenContentType = ObtenerContentType(imagenFile);
            productoDb.ImagenUrl = string.Empty;
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
            categorias, "Id", "Nombre", categoriaSeleccionada);
    }

    private static bool ImagenValida(IFormFile archivo)
    {
        var extensiones = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();

        var tipos = new[]
        {
            "image/jpeg",
            "image/png",
            "image/webp"
        };

        return archivo.Length > 0
            && archivo.Length <= MaxImagenBytes
            && extensiones.Contains(extension)
            && tipos.Contains(archivo.ContentType.ToLowerInvariant());
    }

    private static async Task<byte[]> LeerImagen(IFormFile archivo)
    {
        await using var memory = new MemoryStream();
        await archivo.CopyToAsync(memory);
        return memory.ToArray();
    }

    private static string ObtenerContentType(IFormFile archivo)
    {
        return Path.GetExtension(archivo.FileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.AdminOrOperaciones)]
    public async Task<IActionResult> EditStock(int id)
    {
        var producto = await _context.Productos
            .FirstOrDefaultAsync(p => p.Id == id);

        if (producto == null)
            return NotFound();

        var modelo = new EditarStockDto
        {
            Id = producto.Id,
            Nombre = producto.Nombre,
            StockActual = producto.Stock,
            NuevoStock = producto.Stock
        };

        return View(modelo);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.AdminOrOperaciones)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditStock(EditarStockDto modelo)
    {
        if (!ModelState.IsValid)
            return View(modelo);

        var producto = await _context.Productos
            .FirstOrDefaultAsync(p => p.Id == modelo.Id);

        if (producto == null)
            return NotFound();

        producto.Stock = modelo.NuevoStock;

        await _context.SaveChangesAsync();

        TempData["Mensaje"] = "Stock actualizado correctamente.";

        return RedirectToAction(nameof(Index));
    }
}
