using AccessTrust.Web.Data;
using AccessTrust.Web.Models;
using AccessTrust.Web.Services.Policies;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AccessTrust.Web.Services.Resources;

public class RecursoService : IRecursoService
{
    private readonly IMongoCollection<Recurso> _recursos;
    private readonly IPoliticaAccesoService _politicaAccesoService;

    public RecursoService(
        IMongoDatabase database,
        IPoliticaAccesoService politicaAccesoService)
    {
        _recursos = database.GetCollection<Recurso>(MongoCollections.Recursos);
        _politicaAccesoService = politicaAccesoService;
    }

    public async Task<List<Recurso>> GetAllAsync()
    {
        return await _recursos
            .Find(Builders<Recurso>.Filter.Empty)
            .SortByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Recurso>> GetActivosAsync()
    {
        return await _recursos
            .Find(r => r.Activo)
            .SortBy(r => r.Nombre)
            .ToListAsync();
    }

    public async Task<Recurso?> GetByIdAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return null;
        }

        return await _recursos
            .Find(r => r.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task CreateAsync(Recurso recurso)
    {
        var politica = await _politicaAccesoService
            .GetBySensibilidadAsync(recurso.Sensibilidad);

        recurso.PoliticaId = politica?.Id;
        recurso.CreatedAt = DateTime.UtcNow;

        await _recursos.InsertOneAsync(recurso);
    }

    public async Task<bool> UpdateAsync(string id, Recurso recurso)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return false;
        }

        var politica = await _politicaAccesoService
            .GetBySensibilidadAsync(recurso.Sensibilidad);

        var update = Builders<Recurso>.Update
            .Set(r => r.Nombre, recurso.Nombre)
            .Set(r => r.Tipo, recurso.Tipo)
            .Set(r => r.Sensibilidad, recurso.Sensibilidad)
            .Set(r => r.Activo, recurso.Activo)
            .Set(r => r.PoliticaId, politica?.Id)
            .Set(r => r.UpdatedAt, DateTime.UtcNow);

        var result = await _recursos.UpdateOneAsync(
            r => r.Id == id,
            update
        );

        return result.MatchedCount > 0;
    }
}