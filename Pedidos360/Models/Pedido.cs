using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Pedidos360.Models
{
    public class Pedido
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El pedido debe tener asignado un cliente")]
        public int ClienteId { get; set; }
        public Cliente? Cliente { get; set; }

        [Required(ErrorMessage = "El pedido debe tener asignado un usuario")]
        public string UsuarioId { get; set; } = string.Empty;
        public IdentityUser? Usuario { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        [Range(0, double.MaxValue,ErrorMessage = "El subtotal no puede ser negativo")]
        public decimal Subtotal { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Los impuestos no pueden ser negativos")]
        public decimal Impuestos { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El total no puede ser negativo")]
        public decimal Total { get; set; }

        public enum EstadoPedido
        {
            Pendiente,
            Procesando,
            Completado,
            Cancelado
        }

        public EstadoPedido Estado { get; set; } = EstadoPedido.Pendiente;

        public ICollection<PedidoDetalle> Detalles { get; set; } = new List<PedidoDetalle>();
    }
}
