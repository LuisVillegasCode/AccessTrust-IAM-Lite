namespace AccessTrust.Web.ViewModels.Reports;

public class ReportesFiltroViewModel
{
    public DateTime? FechaDesde { get; set; }

    public DateTime? FechaHasta { get; set; }

    public string? RecursoId { get; set; }

    public string? UsuarioId { get; set; }

    public string? Estado { get; set; }

    public List<ReporteFiltroOpcionViewModel> RecursosDisponibles { get; set; } = new();

    public List<ReporteFiltroOpcionViewModel> UsuariosDisponibles { get; set; } = new();

    public List<string> EstadosDisponibles { get; set; } = new();
}