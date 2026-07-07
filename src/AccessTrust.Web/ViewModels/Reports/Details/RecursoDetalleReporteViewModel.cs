namespace AccessTrust.Web.ViewModels.Reports;

public class RecursoDetalleReporteViewModel
{
    public string Id { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public string Sensibilidad { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;

    public string ResponsableNombre { get; set; } = string.Empty;

    public string ResponsableCorreo { get; set; } = string.Empty;

    public bool Activo { get; set; }

    public DateTime? FechaCreacion { get; set; }
}