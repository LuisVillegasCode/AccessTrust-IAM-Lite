namespace AccessTrust.Web.Services.Audit;

public class AuditoriaFiltro
{
    public string? Accion { get; set; }

    public string? Resultado { get; set; }

    public string? ActorUserId { get; set; }

    public string? EntidadTipo { get; set; }

    public DateTime? FechaDesde { get; set; }

    public DateTime? FechaHasta { get; set; }

    public int Limite { get; set; } = 100;
}