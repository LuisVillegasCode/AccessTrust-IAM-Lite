using AccessTrust.Web.Models;

namespace AccessTrust.Web.Services.Requests;

public interface ISolicitudAccesoService
{
    Task<List<SolicitudAcceso>> GetByUsuarioAsync(string usuarioId);

    Task<List<SolicitudAcceso>> GetPendientesAsync();

    Task<List<SolicitudAcceso>> GetPendientesParaRevisionAsync(
        string revisorId,
        bool esAdministrador
    );

    Task<(bool TienePermiso, string Message)> PuedeRevisarSolicitudAsync(
        string solicitudId,
        string revisorId,
        bool esAdministrador
    );

    Task<SolicitudAcceso?> GetByIdAsync(string id);

    Task<SolicitudAcceso?> GetByIdAndUsuarioAsync(string id, string usuarioId);

    Task<(bool Success, string Message)> CreateAsync(SolicitudAcceso solicitud);

    Task<(bool Success, string Message, string? TokenPlano, string? CredencialId)> AprobarAsync(
        string solicitudId,
        string aprobadorId,
        bool esAdministrador,
        int duracionAprobadaMin,
        string? observacion
    );

    Task<(bool Success, string Message)> RechazarAsync(
        string solicitudId,
        string aprobadorId,
        bool esAdministrador,
        string observacion
    );
}