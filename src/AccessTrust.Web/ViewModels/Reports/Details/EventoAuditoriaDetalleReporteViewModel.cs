namespace AccessTrust.Web.ViewModels.Reports;

public class EventoAuditoriaDetalleReporteViewModel
{
    public string Id { get; set; } = string.Empty;

    public long Seq { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public string ActorNombre { get; set; } = string.Empty;

    public string ActorCorreo { get; set; } = string.Empty;

    public string Accion { get; set; } = string.Empty;

    public string Resultado { get; set; } = string.Empty;

    public string DetalleResumen { get; set; } = string.Empty;

    public string PrevHash { get; set; } = string.Empty;

    public string Hash { get; set; } = string.Empty;
}