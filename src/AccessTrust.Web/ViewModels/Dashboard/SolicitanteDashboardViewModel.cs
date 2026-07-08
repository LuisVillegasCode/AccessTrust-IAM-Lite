namespace AccessTrust.Web.ViewModels.Dashboard;

public class SolicitanteDashboardViewModel
{
    public string UsuarioNombre { get; set; } = string.Empty;

    public string UsuarioCorreo { get; set; } = string.Empty;

    public DateTime GeneradoAt { get; set; } = DateTime.UtcNow;

    public int SolicitudesPendientes { get; set; }

    public int SolicitudesAprobadas { get; set; }

    public int SolicitudesRechazadas { get; set; }

    public int AccesosActivos { get; set; }

    public int AccesosProximosAExpirar { get; set; }

    public List<SolicitudRecienteDashboardItemViewModel> SolicitudesRecientes { get; set; } = new();

    public List<AccesoActivoDashboardItemViewModel> AccesosActivosRecientes { get; set; } = new();
}