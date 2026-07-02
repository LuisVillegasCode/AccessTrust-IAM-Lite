using AccessTrust.Web.Models;

namespace AccessTrust.Web.Services.Policies;

public interface IPoliticaAccesoService
{
    Task<List<PoliticaAcceso>> GetAllAsync();
    Task<PoliticaAcceso?> GetByIdAsync(string id);
    Task<PoliticaAcceso?> GetBySensibilidadAsync(SensibilidadRecurso sensibilidad);
    Task<bool> UpdateAsync(string id, PoliticaAcceso politica);
}