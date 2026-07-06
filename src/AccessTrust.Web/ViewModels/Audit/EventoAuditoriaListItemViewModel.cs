using AccessTrust.Web.Models;

namespace AccessTrust.Web.ViewModels.Audit;

public class EventoAuditoriaListItemViewModel
{
    public string Id { get; set; } = string.Empty;

    public long Seq { get; set; }

    public string? ActorUserId { get; set; }

    public string Accion { get; set; } = string.Empty;

    public string EntidadTipo { get; set; } = string.Empty;

    public string? EntidadId { get; set; }

    public ResultadoAuditoria Resultado { get; set; }

    public DateTime CreatedAt { get; set; }

    public string HashResumen =>
        string.IsNullOrWhiteSpace(Hash)
            ? "No disponible"
            : Hash.Length <= 12
                ? Hash
                : $"{Hash[..12]}...";

    public string Hash { get; set; } = string.Empty;
}