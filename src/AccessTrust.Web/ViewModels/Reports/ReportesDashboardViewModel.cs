namespace AccessTrust.Web.ViewModels.Reports;

public class ReportesDashboardViewModel
{
    public DateTime GeneradoAt { get; set; } = DateTime.UtcNow;

    public ReportesFiltroViewModel Filtro { get; set; } = new();

    public List<ReporteConteoPorEstadoViewModel> SolicitudesPorEstado { get; set; } = new();

    public List<ReporteConteoPorRecursoViewModel> SolicitudesPorRecurso { get; set; } = new();

    public List<ReporteConteoPorEstadoViewModel> CredencialesPorEstado { get; set; } = new();

    public List<ReporteConteoPorRecursoViewModel> CredencialesPorRecurso { get; set; } = new();

    public List<ReporteConteoPorEstadoViewModel> TicketsExternosPorEstado { get; set; } = new();

    public List<ReporteAuditoriaAccionResultadoViewModel> EventosAuditoriaPorAccionResultado { get; set; } = new();

    public List<ReporteAccesoExternoViewModel> AccesosExternosPermitidosDenegados { get; set; } = new();

    public List<ReporteRecursoPorSensibilidadViewModel> RecursosPorSensibilidad { get; set; } = new();

    public int TotalSolicitudes =>
        SolicitudesPorEstado.Sum(x => x.Total);

    public int TotalCredenciales =>
        CredencialesPorEstado.Sum(x => x.Total);

    public int TotalTicketsExternos =>
        TicketsExternosPorEstado.Sum(x => x.Total);

    public int TotalEventosAuditoria =>
        EventosAuditoriaPorAccionResultado.Sum(x => x.Total);

    public int TotalRecursos =>
        RecursosPorSensibilidad.Sum(x => x.Total);
}