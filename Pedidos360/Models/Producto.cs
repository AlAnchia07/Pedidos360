using System.ComponentModel.DataAnnotations;

namespace Pedidos360.Models
{
    public class Producto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El producto debe tener un nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El producto debe tener una categoría")]
        public int CategoriaId { get; set; }
        public Categoria? Categoria { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "El precio del producto debe ser mayor a 0")]
        public decimal Precio { get; set; }

        [Range(0, 100, ErrorMessage = "El porcentaje de impuesto del producto debe estar entre 0 y 100")]
        public decimal ImpuestoPorc { get; set; }

        [Required(ErrorMessage = "El producto debe tener una cantidad en stock")]
        [Range(0, int.MaxValue, ErrorMessage = "La cantidad en stock del producto no puede ser negativa")]
        public int Stock { get; set; }

        [Required(ErrorMessage = "El producto debe tener una imagen")]
        public string ImagenUrl { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;

    }
}
