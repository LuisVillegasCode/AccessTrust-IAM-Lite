using AccessTrust.Web.Models;

namespace AccessTrust.Web.ViewModels.Audit;

public class EventoAuditoriaDetailsViewModel
{
    public string Id { get; set; } = string.Empty;

    public long Seq { get; set; }

    public string? ActorUserId { get; set; }

    public string Accion { get; set; } = string.Empty;

    public string EntidadTipo { get; set; } = string.Empty;

    public string? EntidadId { get; set; }

    public ResultadoAuditoria Resultado { get; set; }

    public Dictionary<string, string> Detalle { get; set; } = new();

    public string PrevHash { get; set; } = string.Empty;

    public string Hash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}