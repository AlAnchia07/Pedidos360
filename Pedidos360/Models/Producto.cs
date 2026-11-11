using System.ComponentModel.DataAnnotations;

namespace Pedidos360.Models
{
    public class Producto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El producto debe tener un nombre")]
        [StringLength(150, MinimumLength = 2)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El producto debe tener una categoría")]
        public int CategoriaId { get; set; }

        public Categoria? Categoria { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Precio { get; set; }

        [Range(0, 100, ErrorMessage = "El impuesto debe estar entre 0 y 100")]
        public decimal ImpuestoPorc { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
        [ConcurrencyCheck]
        public int Stock { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El stock mínimo no puede ser negativo")]
        public int StockMinimo { get; set; } = 0;

        // Nueva imagen almacenada directamente en SQL Server
        public byte[]? ImagenDatos { get; set; }

        public string? ImagenContentType { get; set; }

        // Se conserva para compatibilidad con imágenes anteriores
        public string ImagenUrl { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;
    }
}


