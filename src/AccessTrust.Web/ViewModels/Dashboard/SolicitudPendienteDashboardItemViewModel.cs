namespace AccessTrust.Web.ViewModels.Dashboard;

public class SolicitudPendienteDashboardItemViewModel
{
    public string Id { get; set; } = string.Empty;

    public string UsuarioId { get; set; } = string.Empty;

    public string UsuarioNombre { get; set; } = string.Empty;

    public string UsuarioCorreo { get; set; } = string.Empty;

    public string RecursoNombre { get; set; } = string.Empty;

    public string RecursoTipo { get; set; } = string.Empty;

    public string Sensibilidad { get; set; } = string.Empty;

    public string Prioridad { get; set; } = string.Empty;

    public int DuracionSolicitadaMin { get; set; }

    public DateTime CreatedAt { get; set; }
}