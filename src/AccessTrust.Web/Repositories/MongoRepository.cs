using AccessTrust.Web.Data;
using AccessTrust.Web.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AccessTrust.Web.Repositories;

public class MongoRepository<T> : IMongoRepository<T> where T : class
{
    private readonly IMongoCollection<T> _collection;

    public MongoRepository(IMongoDatabase database)
    {
        var collectionName = GetCollectionName();
        _collection = database.GetCollection<T>(collectionName);
    }

    public async Task<List<T>> GetAllAsync()
    {
        return await _collection.Find(Builders<T>.Filter.Empty).ToListAsync();
    }

    public async Task<T?> GetByIdAsync(string id)
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return null;
        }

        var filter = Builders<T>.Filter.Eq("_id", objectId);
        return await _collection.Find(filter).FirstOrDefaultAsync();
    }

    public async Task CreateAsync(T entity)
    {
        await _collection.InsertOneAsync(entity);
    }

    public async Task<bool> ReplaceAsync(string id, T entity)
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return false;
        }

        var filter = Builders<T>.Filter.Eq("_id", objectId);
        var result = await _collection.ReplaceOneAsync(filter, entity);

        return result.MatchedCount > 0;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        if (!ObjectId.TryParse(id, out var objectId))
        {
            return false;
        }

        var filter = Builders<T>.Filter.Eq("_id", objectId);
        var result = await _collection.DeleteOneAsync(filter);

        return result.DeletedCount > 0;
    }

    private static string GetCollectionName()
    {
        return typeof(T).Name switch
        {
            nameof(Usuario) => MongoCollections.Usuarios,
            nameof(Rol) => MongoCollections.Roles,
            nameof(Recurso) => MongoCollections.Recursos,
            nameof(PoliticaAcceso) => MongoCollections.PoliticasAcceso,
            nameof(SolicitudAcceso) => MongoCollections.SolicitudesAcceso,
            nameof(CredencialTemporal) => MongoCollections.CredencialesTemporales,
            nameof(OtpCodigo) => MongoCollections.OtpCodigos,
            nameof(EventoAuditoria) => MongoCollections.EventosAuditoria,
            nameof(AlertaSeguridad) => MongoCollections.AlertasSeguridad,
            nameof(SesionUsuario) => MongoCollections.Sesiones,
            _ => throw new InvalidOperationException(
                $"No se ha definido una colección MongoDB para el modelo {typeof(T).Name}.")
        };
    }
}