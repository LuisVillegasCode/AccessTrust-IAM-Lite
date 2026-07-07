namespace AccessTrust.Web.ViewModels.Reports;

public class SolicitudDetalleReporteViewModel
{
    public string Id { get; set; } = string.Empty;

    public DateTime? FechaCreacion { get; set; }

    public string UsuarioNombre { get; set; } = string.Empty;

    public string UsuarioCorreo { get; set; } = string.Empty;

    public string RecursoNombre { get; set; } = string.Empty;

    public string RecursoTipo { get; set; } = string.Empty;

    public string Sensibilidad { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public string Motivo { get; set; } = string.Empty;

    public string AprobadorNombre { get; set; } = string.Empty;

    public DateTime? FechaDecision { get; set; }
}