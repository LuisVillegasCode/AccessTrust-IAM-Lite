namespace AccessTrust.Web.ViewModels.Reports;

public class ReportesFiltroViewModel
{
    // ==========================
    // Filtros globales
    // ==========================

    public DateTime? FechaDesde { get; set; }

    public DateTime? FechaHasta { get; set; }

    public string? RecursoId { get; set; }

    public string? UsuarioId { get; set; }

    public string? UsuarioTexto { get; set; }

    // ==========================
    // Filtros contextuales
    // ==========================

    public string? EstadoSolicitud { get; set; }

    public string? EstadoCredencial { get; set; }

    public string? EstadoTicket { get; set; }

    public string? ResultadoAuditoria { get; set; }

    public string? AccionAuditoria { get; set; }

    public string? SensibilidadRecurso { get; set; }

    // ==========================
    // Opciones para filtros
    // ==========================

    public List<ReporteFiltroOpcionViewModel> RecursosDisponibles { get; set; } = new();

    public List<ReporteFiltroOpcionViewModel> UsuariosDisponibles { get; set; } = new();

    public List<string> EstadosSolicitudDisponibles { get; set; } = new();

    public List<string> EstadosCredencialDisponibles { get; set; } = new();

    public List<string> EstadosTicketDisponibles { get; set; } = new();

    public List<string> ResultadosAuditoriaDisponibles { get; set; } = new();

    public List<string> SensibilidadesRecursoDisponibles { get; set; } = new();

    // ==========================
    // Compatibilidad temporal
    // Se eliminarán cuando se actualice
    // ReporteService e Index.cshtml.
    // ==========================

    public string? Estado { get; set; }

    public List<string> EstadosDisponibles { get; set; } = new();
}