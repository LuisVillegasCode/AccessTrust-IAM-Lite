using System.ComponentModel.DataAnnotations;

namespace AccessTrust.Web.ViewModels.Approvals;

public class RechazarSolicitudViewModel
{
    [Required]
    public string SolicitudId { get; set; } = string.Empty;

    [Required(ErrorMessage = "La observación es obligatoria para rechazar la solicitud.")]
    [StringLength(500, ErrorMessage = "La observación no puede superar los 500 caracteres.")]
    public string Observacion { get; set; } = string.Empty;
}