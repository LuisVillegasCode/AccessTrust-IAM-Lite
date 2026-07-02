using AccessTrust.Web.Models;

namespace AccessTrust.Web.Services.Audit;

public interface IAuditService
{
    Task RegistrarEventoAsync(
        string accion,
        string entidadTipo,
        ResultadoAuditoria resultado,
        string? actorUserId = null,
        string? entidadId = null,
        Dictionary<string, string>? detalle = null
    );
}