using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pedidos360.Data;
using Pedidos360.DTOs;

namespace Pedidos360.Controllers.Api
{
    [Route("api/pedidos")]
    [ApiController]
    [Authorize]// Requerir autenticación como mínimo
    public class PedidosApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PedidosApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: /api/pedidos/calcular

        //Los descuentos se aplican a la línea, no unitariamente
        //El endpoint recibe un DTO PedidoCalculoDto que contiene una lista de LineaPedidoDto que contiene productoId, cantidad, descuento
        [HttpPost("calcular")]
        public async Task<ActionResult> Calcular([FromBody] PedidoCalculoDto pedido)
        {
            if(pedido == null || pedido.Lineas == null || !pedido.Lineas.Any())
            {
                return BadRequest("El pedido no puede estar vacío para su cálculo");
            }

            decimal subtotal = 0;
            decimal impuestos = 0;

            foreach(var linea in pedido.Lineas)
            {
                var producto = await _context.Productos.FindAsync(linea.ProductoId);

                if(producto == null)
                {
                    return NotFound($"Producto con ID {linea.ProductoId} no encontrado");
                }

                if(linea.Cantidad <= 0)
                {
                    return BadRequest($"La cantidad para el producto {producto.Nombre} debe ser mayor que cero");
                }

                if(linea.Descuento < 0)
                {
                    return BadRequest($"El descuento para el producto {producto.Nombre} no puede ser negativo");
                }

                if(linea.Descuento > (producto.Precio * linea.Cantidad))
                {
                    return BadRequest($"El descuento para el producto {producto.Nombre} no puede ser mayor que el total de la línea");
                }

                subtotal += (producto.Precio * linea.Cantidad) - linea.Descuento;

                impuestos += ((producto.Precio * linea.Cantidad) - linea.Descuento) * (producto.ImpuestoPorc / 100);

            }

            decimal total = subtotal + impuestos;

            return Ok(new 
            {
                Subtotal = subtotal,
                Impuestos = impuestos,
                Total = total
            });

        }
}
}
