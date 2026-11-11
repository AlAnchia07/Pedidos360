
using System.ComponentModel.DataAnnotations;

namespace Pedidos360.DTOs
{
    public class PedidoCrearDto
    {
        [Required(ErrorMessage = "Debe seleccionar un cliente")]
        public int ClienteId { get; set; }

        public List<LineaPedidoDto> Lineas { get; set; } = new();
    }
}
