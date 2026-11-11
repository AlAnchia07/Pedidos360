using System.ComponentModel.DataAnnotations;
using Pedidos360.Security;

namespace Pedidos360.Models.ViewModels;

public class CreateUserViewModel
{
    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Escribe un correo válido.")]
    [Display(Name = "Correo electrónico")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 10, ErrorMessage = "La contraseña debe tener al menos 10 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecciona un rol.")]
    [RegularExpression("^(Admin|Ventas|Operaciones)$", ErrorMessage = "El rol seleccionado no es válido.")]
    [Display(Name = "Rol")]
    public string Role { get; set; } = RoleNames.Ventas;
}
