using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Pedidos360.Data;
using Pedidos360.Models;

namespace Pedidos360.Controllers;

public class ProductosController : Controller
{
    private readonly ApplicationDbContext _context;
    private const int PageSize = 10;

    public ProductosController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(
        string? nombre,
        int? categoriaId,
        int page = 1)
    {
        if (page < 1) page = 1;

        var query = _context.Productos
            .Include(p => p.Categoria)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(nombre))
        {
            query = query.Where(p => p.Nombre.Contains(nombre));
        }

        if (categoriaId.HasValue && categoriaId > 0)
        {
            query = query.Where(p => p.CategoriaId == categoriaId);
        }

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));

        if (page > totalPages) page = totalPages;

        var productos = await query
            .OrderBy(p => p.Nombre)
            .Skip((page - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        ViewData["BuscarNombre"] = nombre;
        ViewData["CategoriaActual"] = categoriaId;
        ViewData["Page"] = page;
        ViewData["TotalPages"] = totalPages;
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

    public async Task<IActionResult> Create()
    {
        await CargarCategorias();
        return View();
    }

    [HttpPost]
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

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var producto = await _context.Productos.FindAsync(id);
        if (producto == null) return NotFound();

        await CargarCategorias(producto.CategoriaId);
        return View(producto);
    }

    [HttpPost]
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

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var producto = await _context.Productos
            .Include(p => p.Categoria)
            .FirstOrDefaultAsync(p => p.Id == id);

        return producto == null ? NotFound() : View(producto);
    }

    [HttpPost, ActionName("Delete")]
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