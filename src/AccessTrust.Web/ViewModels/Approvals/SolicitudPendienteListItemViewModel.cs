using AccessTrust.Web.Models;

namespace AccessTrust.Web.ViewModels.Approvals;

public class SolicitudPendienteListItemViewModel
{
    public string Id { get; set; } = string.Empty;

    public string UsuarioId { get; set; } = string.Empty;

    public string RecursoId { get; set; } = string.Empty;

    public string RecursoNombre { get; set; } = string.Empty;

    public string RecursoTipo { get; set; } = string.Empty;

    public SensibilidadRecurso RecursoSensibilidad { get; set; }

    public string Motivo { get; set; } = string.Empty;

    public int DuracionSolicitadaMin { get; set; }

    public string Prioridad { get; set; } = "Normal";

    public EstadoSolicitud Estado { get; set; }

    public DateTime CreatedAt { get; set; }

    public string UsuarioNombre { get; set; } = string.Empty;

    public string UsuarioCorreo { get; set; } = string.Empty;
}