namespace AccessTrust.Web.ViewModels.Dashboard;

public class AprobadorDashboardViewModel
{
    public string UsuarioNombre { get; set; } = string.Empty;

    public string UsuarioCorreo { get; set; } = string.Empty;

    public DateTime GeneradoAt { get; set; } = DateTime.UtcNow;

    public int SolicitudesPendientesAsignadas { get; set; }

    public int RecursosBajoResponsabilidad { get; set; }

    public int SolicitudesAprobadasRecientes { get; set; }

    public int SolicitudesRechazadasRecientes { get; set; }

    public int CredencialesEmitidasRecientes { get; set; }

    public List<SolicitudPendienteDashboardItemViewModel> SolicitudesPendientes { get; set; } = new();

    public List<RecursoResponsableDashboardItemViewModel> RecursosResponsable { get; set; } = new();

    public List<EventoAuditoriaDashboardItemViewModel> ActividadReciente { get; set; } = new();
}