using AccessTrust.Web.Models;

namespace AccessTrust.Web.Services.Resources;

public interface IRecursoService
{
    Task<List<Recurso>> GetAllAsync();
    Task<List<Recurso>> GetActivosAsync();
    Task<Recurso?> GetByIdAsync(string id);
    Task CreateAsync(Recurso recurso);
    Task<bool> UpdateAsync(string id, Recurso recurso);
}