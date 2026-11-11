using System.ComponentModel.DataAnnotations;

namespace Pedidos360.Models
{
    public class Categoria
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La categoría debe tener un nombre")]
        [StringLength(100, MinimumLength = 2)]
        public string Nombre { get; set; } = string.Empty;

        public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}
