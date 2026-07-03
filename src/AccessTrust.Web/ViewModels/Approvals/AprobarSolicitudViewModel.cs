using System.ComponentModel.DataAnnotations;

namespace AccessTrust.Web.ViewModels.Approvals;

public class AprobarSolicitudViewModel
{
    [Required]
    public string SolicitudId { get; set; } = string.Empty;

    [Required(ErrorMessage = "La duración aprobada es obligatoria.")]
    [Range(1, 480, ErrorMessage = "La duración aprobada debe estar entre 1 y 480 minutos.")]
    public int DuracionAprobadaMin { get; set; }

    [StringLength(500, ErrorMessage = "La observación no puede superar los 500 caracteres.")]
    public string? Observacion { get; set; }
}