
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pedidos360.Data;
using Pedidos360.DTOs;
using Pedidos360.Models;
using Pedidos360.Security;

namespace Pedidos360.Controllers
{
    [Authorize(Roles = RoleNames.AllRoles)]
    public class PedidosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public PedidosController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Pedidos

        public async Task<IActionResult> Index(string? buscar)
        {
            var consulta = _context.Pedidos
                .Include(p => p.Cliente)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                if (int.TryParse(buscar, out int numeroPedido))
                {
                    consulta = consulta.Where(p => p.Id == numeroPedido);
                }
                else
                {
                    consulta = consulta.Where(p =>
                        p.Cliente != null &&
                        p.Cliente.Nombre.Contains(buscar));
                }
            }

            var pedidos = await consulta
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();

            ViewBag.Buscar = buscar;

            return View(pedidos);
        }


        // GET: Pedidos/Create
        [Authorize(Roles = RoleNames.AdminAndSales)]
        public async Task<IActionResult> Create()
        {
            ViewBag.Clientes = await _context.Clientes
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            return View();
        }

        // POST: Pedidos/Create
        [HttpPost]
        [Authorize(Roles = RoleNames.AdminAndSales)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] PedidoCrearDto dto)
        {
            // Validar que se haya seleccionado un cliente
            if (dto == null || dto.ClienteId <= 0)
            {
                return BadRequest(new { mensaje = "Debe seleccionar un cliente." });
            }

            // Validar que existan productos
            if (dto.Lineas == null || !dto.Lineas.Any())
            {
                return BadRequest(new { mensaje = "Debe agregar al menos un producto." });
            }

            // Validar que no haya productos repetidos
            if (dto.Lineas.GroupBy(l => l.ProductoId).Any(g => g.Count() > 1))
            {
                return BadRequest(new { mensaje = "Hay productos repetidos en el pedido." });
            }

            // Verificar que el cliente exista
            var cliente = await _context.Clientes.FindAsync(dto.ClienteId);

            if (cliente == null)
            {
                return BadRequest(new { mensaje = "El cliente seleccionado no existe." });
            }

            // Obtener el usuario autenticado
            var usuarioId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(usuarioId))
            {
                return Unauthorized(new { mensaje = "Debe iniciar sesión." });
            }

            decimal subtotal = 0;
            decimal impuestos = 0;

            var detalles = new List<PedidoDetalle>();

            // Validar productos y calcular los montos desde la base de datos
            foreach (var linea in dto.Lineas)
            {
                if (linea.ProductoId <= 0 || linea.Cantidad <= 0 || linea.Descuento < 0)
                {
                    return BadRequest(new { mensaje = "Los datos de los productos no son válidos." });
                }

                var producto = await _context.Productos
                    .FirstOrDefaultAsync(p => p.Id == linea.ProductoId);

                if (producto == null || !producto.Activo)
                {
                    return BadRequest(new { mensaje = "Uno de los productos no existe o está inactivo." });
                }

                if (producto.Stock < linea.Cantidad)
                {
                    return BadRequest(new
                    {
                        mensaje = $"No hay suficiente inventario para {producto.Nombre}. Stock disponible: {producto.Stock}."
                    });
                }

                decimal subtotalLinea = producto.Precio * linea.Cantidad;

                if (linea.Descuento > subtotalLinea)
                {
                    return BadRequest(new { mensaje = $"El descuento de {producto.Nombre} supera su subtotal." });
                }

                subtotalLinea -= linea.Descuento;

                decimal impuestoLinea = subtotalLinea * (producto.ImpuestoPorc / 100);
                decimal totalLinea = subtotalLinea + impuestoLinea;

                subtotal += subtotalLinea;
                impuestos += impuestoLinea;

                detalles.Add(new PedidoDetalle
                {
                    ProductoId = producto.Id,
                    Cantidad = linea.Cantidad,
                    PrecioUnit = producto.Precio,
                    Descuento = linea.Descuento,
                    ImpuestoPorc = producto.ImpuestoPorc,
                    TotalLinea = totalLinea
                });
            }

            // Crear el pedido y guardar sus detalles
            using var transaccion = await _context.Database.BeginTransactionAsync();

            try
            {
                var pedido = new Pedido
                {
                    ClienteId = dto.ClienteId,
                    UsuarioId = usuarioId,
                    Fecha = DateTime.Now,
                    Subtotal = subtotal,
                    Impuestos = impuestos,
                    Total = subtotal + impuestos,
                    Estado = Pedido.EstadoPedido.Pendiente,
                    Detalles = detalles
                };

                _context.Pedidos.Add(pedido);

                // Descontar el inventario
                foreach (var detalle in detalles)
                {
                    var producto = await _context.Productos.FindAsync(detalle.ProductoId);
                    producto!.Stock -= detalle.Cantidad;
                }

                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                return Ok(new
                {
                    mensaje = "Pedido registrado correctamente.",
                    pedidoId = pedido.Id
                });
            }

            catch (DbUpdateConcurrencyException)
            {
                await transaccion.RollbackAsync();

                return Conflict(new
                {
                    mensaje = "El inventario fue modificado por otro usuario mientras se registraba el pedido. Por favor, actualice la página e intente nuevamente."
                });
            }
            catch
            {
                await transaccion.RollbackAsync();
                throw;
            }

        }

        // GET: Pedidos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var pedido = await _context.Pedidos
                .Include(p => p.Cliente)
                .Include(p => p.Usuario)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
            {
                return NotFound();
            }

            return View(pedido);
        }




    }
}
