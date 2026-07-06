using System.ComponentModel.DataAnnotations;
using AccessTrust.Web.Models;

namespace AccessTrust.Web.ViewModels.Credentials;

public class RevocarCredencialViewModel
{
    public string CredencialId { get; set; } = string.Empty;

    public string UsuarioId { get; set; } = string.Empty;

    public string RecursoId { get; set; } = string.Empty;

    public string RecursoNombre { get; set; } = string.Empty;

    public string RecursoTipo { get; set; } = string.Empty;

    public SensibilidadRecurso RecursoSensibilidad { get; set; }

    public EstadoCredencial Estado { get; set; }

    public DateTime IssuedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public int UsosRealizados { get; set; }

    public int MaxUsos { get; set; }

    [Required(ErrorMessage = "El motivo de revocación es obligatorio.")]
    [StringLength(300, MinimumLength = 5, ErrorMessage = "El motivo debe tener entre 5 y 300 caracteres.")]
    [Display(Name = "Motivo de revocación")]
    public string MotivoRevocacion { get; set; } = string.Empty;
}