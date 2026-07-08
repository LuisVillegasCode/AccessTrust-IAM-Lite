using AccessTrust.Web.Models;

namespace AccessTrust.Web.Services.Resources;

public interface IRecursoService
{
    Task<List<Recurso>> GetAllAsync();

    Task<List<Recurso>> GetActivosAsync();

    Task<Recurso?> GetByIdAsync(string id);

    Task<List<Usuario>> GetAprobadoresActivosAsync();

    Task CreateAsync(Recurso recurso);

    Task<(bool Success, string Message)> UpdateAsync(
        string id,
        Recurso recurso,
        string actorUserId
    );
}