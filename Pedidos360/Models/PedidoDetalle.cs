using System.ComponentModel.DataAnnotations;
namespace Pedidos360.Models
{
    public class PedidoDetalle
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El detalle del pedido debe tener asignado un pedido")]
        public int PedidoId { get; set; }
        public Pedido? Pedido { get; set; }

        [Required(ErrorMessage = "El detalle del pedido debe tener asignado un producto")]
        public int ProductoId { get; set; }
        public Producto? Producto { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La cantidad del producto en el detalle del pedido debe ser mayor a 0")]
        public int Cantidad { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "El precio del producto en el detalle del pedido debe ser mayor a 0")]
        public decimal PrecioUnit { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "El descuento no puede ser negativo")]
        public decimal Descuento { get; set; }

        [Range(0, 100, ErrorMessage = "El porcentaje de impuesto del detalle debe estar entre 0 y 100")]
        public decimal ImpuestoPorc { get; set; }

        [Range(0.01, double.MaxValue,ErrorMessage = "El total de la línea debe ser mayor a 0")]
        public decimal TotalLinea { get; set; }
    }
}
