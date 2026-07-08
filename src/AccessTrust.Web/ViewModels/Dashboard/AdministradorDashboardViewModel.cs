namespace AccessTrust.Web.ViewModels.Dashboard;

public class AdministradorDashboardViewModel
{
    public string UsuarioNombre { get; set; } = string.Empty;

    public string UsuarioCorreo { get; set; } = string.Empty;

    public DateTime GeneradoAt { get; set; } = DateTime.UtcNow;

    public int UsuariosActivos { get; set; }

    public int RecursosActivos { get; set; }

    public int SolicitudesPendientesTotales { get; set; }

    public int CredencialesActivas { get; set; }

    public int TicketsExternosEmitidos { get; set; }

    public int EventosAuditoriaHoy { get; set; }

    public int EventosFallidosRecientes { get; set; }

    public int IntentosNoAutorizadosRecientes { get; set; }

    public bool IntegridadAuditoriaValida { get; set; } = true;

    public List<EventoAuditoriaDashboardItemViewModel> EventosRecientes { get; set; } = new();

    public List<AlertaSeguridadDashboardItemViewModel> AlertasRecientes { get; set; } = new();

    public List<RecursoCriticoDashboardItemViewModel> RecursosCriticos { get; set; } = new();
}