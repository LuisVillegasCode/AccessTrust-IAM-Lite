namespace AccessTrust.Web.ViewModels.Dashboard;

public class EventoAuditoriaDashboardItemViewModel
{
    public string Id { get; set; } = string.Empty;

    public long Seq { get; set; }

    public string ActorUserId { get; set; } = string.Empty;

    public string Accion { get; set; } = string.Empty;

    public string EntidadTipo { get; set; } = string.Empty;

    public string EntidadId { get; set; } = string.Empty;

    public string Resultado { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}