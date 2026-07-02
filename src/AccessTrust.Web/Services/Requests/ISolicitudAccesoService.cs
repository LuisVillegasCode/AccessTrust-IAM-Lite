using AccessTrust.Web.Models;

namespace AccessTrust.Web.Services.Requests;

public interface ISolicitudAccesoService
{
    Task<List<SolicitudAcceso>> GetByUsuarioAsync(string usuarioId);

    Task<List<SolicitudAcceso>> GetPendientesAsync();

    Task<SolicitudAcceso?> GetByIdAsync(string id);

    Task<SolicitudAcceso?> GetByIdAndUsuarioAsync(string id, string usuarioId);

    Task<(bool Success, string Message)> CreateAsync(SolicitudAcceso solicitud);
}