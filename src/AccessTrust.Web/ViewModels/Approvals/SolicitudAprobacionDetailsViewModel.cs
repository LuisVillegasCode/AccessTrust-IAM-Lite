using AccessTrust.Web.Models;

namespace AccessTrust.Web.ViewModels.Approvals;

public class SolicitudAprobacionDetailsViewModel
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

    public string PoliticaNombre { get; set; } = string.Empty;

    public int PoliticaDuracionMaxMin { get; set; }

    public bool PoliticaRequiereOtp { get; set; }

    public int PoliticaMaxUsos { get; set; }

    public int DuracionAprobadaMin { get; set; }

    public string? Observacion { get; set; }
}