using System.ComponentModel.DataAnnotations;
using AccessTrust.Web.Models;

namespace AccessTrust.Web.ViewModels.Users;

public class CrearUsuarioAdminViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(120, ErrorMessage = "El nombre no debe superar los 120 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "Debe ingresar un correo válido.")]
    [StringLength(160, ErrorMessage = "El correo no debe superar los 160 caracteres.")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El documento de identidad es obligatorio.")]
    [StringLength(20, MinimumLength = 6, ErrorMessage = "El documento debe tener entre 6 y 20 caracteres.")]
    public string DocumentoIdentidad { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña temporal es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    public string PasswordTemporal { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe confirmar la contraseña temporal.")]
    [DataType(DataType.Password)]
    [Compare(nameof(PasswordTemporal), ErrorMessage = "Las contraseñas no coinciden.")]
    public string ConfirmarPasswordTemporal { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe seleccionar el estado de la cuenta.")]
    public EstadoCuenta Estado { get; set; } = EstadoCuenta.Activo;

    public bool RolSolicitante { get; set; } = true;

    public bool RolAprobador { get; set; }

    public bool RolAdministrador { get; set; }

    public List<string> ObtenerRolesSeleccionados()
    {
        var roles = new List<string>();

        if (RolSolicitante)
        {
            roles.Add("Solicitante");
        }

        if (RolAprobador)
        {
            roles.Add("Aprobador");
        }

        if (RolAdministrador)
        {
            roles.Add("Administrador");
        }

        return roles;
    }
}