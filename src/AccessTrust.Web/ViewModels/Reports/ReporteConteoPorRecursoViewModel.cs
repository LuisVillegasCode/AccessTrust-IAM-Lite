namespace AccessTrust.Web.ViewModels.Reports;

public class ReporteConteoPorRecursoViewModel
{
    public string RecursoId { get; set; } = string.Empty;

    public string RecursoNombre { get; set; } = string.Empty;

    public string RecursoTipo { get; set; } = string.Empty;

    public string Sensibilidad { get; set; } = string.Empty;

    public int Total { get; set; }
}