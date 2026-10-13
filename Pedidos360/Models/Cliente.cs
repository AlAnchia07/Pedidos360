
using System.ComponentModel.DataAnnotations;

namespace Pedidos360.Models
{
    public class Cliente
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El cliente debe tener un nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El cliente debe tener una cédula física o jurídica")]
        public string Cedula { get; set; } = string.Empty;

        [Required(ErrorMessage = "El cliente debe tener un correo electrónico")]
        [EmailAddress(ErrorMessage = "El correo electrónico no es válido")]
        public string Correo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El cliente debe tener un teléfono")]
        [Phone(ErrorMessage = "El teléfono no es válido")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "El cliente debe tener una dirección")]
        public string Direccion { get; set; } = string.Empty;
    }
}
