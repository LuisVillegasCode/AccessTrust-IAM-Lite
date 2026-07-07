namespace AccessTrust.Web.ViewModels.Reports;

public class TicketDetalleReporteViewModel
{
    public string Id { get; set; } = string.Empty;

    public DateTime? FechaEmision { get; set; }

    public DateTime? FechaExpiracion { get; set; }

    public DateTime? FechaConsumo { get; set; }

    public string UsuarioNombre { get; set; } = string.Empty;

    public string UsuarioCorreo { get; set; } = string.Empty;

    public string RecursoNombre { get; set; } = string.Empty;

    public string RecursoTipo { get; set; } = string.Empty;

    public string Sensibilidad { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public string CredencialId { get; set; } = string.Empty;
}