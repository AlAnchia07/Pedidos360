using System.ComponentModel.DataAnnotations;

namespace Pedidos360.Models
{
    public class Categoria
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La categoria debe tener un nombre")]
        public string Nombre { get; set; } = string.Empty;
    }
}
