using AccessTrust.Web.Models;
using MongoDB.Driver;

namespace AccessTrust.Web.Services.Credentials;

public interface ICredencialTemporalService
{
    Task<(bool Success, string Message, CredencialTemporal? Credencial, string? TokenPlano)> EmitirAsync(
        SolicitudAcceso solicitud,
        PoliticaAcceso politica,
        int duracionAprobadaMin,
        IClientSessionHandle? session = null
    );

    Task<CredencialTemporal?> GetByIdAsync(string id);

    Task<List<CredencialTemporal>> GetByUsuarioAsync(string usuarioId);
    Task<ValidacionCredencialResult> ValidarTokenAsync(
    string recursoId,
    string tokenPlano
    );
}