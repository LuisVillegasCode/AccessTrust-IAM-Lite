namespace AccessTrust.Web.ViewModels.Audit;

public class AuditoriaIndexViewModel
{
    public string? Accion { get; set; }

    public string? Resultado { get; set; }

    public string? ActorUserId { get; set; }

    public string? EntidadTipo { get; set; }

    public DateTime? FechaDesde { get; set; }

    public DateTime? FechaHasta { get; set; }

    public List<EventoAuditoriaListItemViewModel> Eventos { get; set; } = new();
}