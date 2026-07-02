using AccessTrust.Web.Data;
using AccessTrust.Web.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AccessTrust.Web.Services.Policies;

public class PoliticaAccesoService : IPoliticaAccesoService
{
    private readonly IMongoCollection<PoliticaAcceso> _politicas;

    public PoliticaAccesoService(IMongoDatabase database)
    {
        _politicas = database.GetCollection<PoliticaAcceso>(MongoCollections.PoliticasAcceso);
    }

    public async Task<List<PoliticaAcceso>> GetAllAsync()
    {
        return await _politicas
            .Find(Builders<PoliticaAcceso>.Filter.Empty)
            .SortBy(p => p.Sensibilidad)
            .ToListAsync();
    }

    public async Task<PoliticaAcceso?> GetByIdAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return null;
        }

        return await _politicas
            .Find(p => p.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<PoliticaAcceso?> GetBySensibilidadAsync(SensibilidadRecurso sensibilidad)
    {
        return await _politicas
            .Find(p => p.Sensibilidad == sensibilidad)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateAsync(string id, PoliticaAcceso politica)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return false;
        }

        var update = Builders<PoliticaAcceso>.Update
            .Set(p => p.Nombre, politica.Nombre)
            .Set(p => p.DuracionMaxMin, politica.DuracionMaxMin)
            .Set(p => p.RequiereOtp, politica.RequiereOtp)
            .Set(p => p.MaxUsos, politica.MaxUsos)
            .Set(p => p.RequiereAprobacion, politica.RequiereAprobacion)
            .Set(p => p.IntentosOtpMax, politica.IntentosOtpMax);

        var result = await _politicas.UpdateOneAsync(
            p => p.Id == id,
            update
        );

        return result.MatchedCount > 0;
    }
}