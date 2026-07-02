using AccessTrust.Web.Models;

namespace AccessTrust.Web.ViewModels.Requests;

public class SolicitudAccesoListItemViewModel
{
    public string Id { get; set; } = string.Empty;

    public string RecursoNombre { get; set; } = string.Empty;

    public SensibilidadRecurso RecursoSensibilidad { get; set; }

    public string Motivo { get; set; } = string.Empty;

    public int DuracionSolicitadaMin { get; set; }

    public string Prioridad { get; set; } = "Normal";

    public EstadoSolicitud Estado { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }
}