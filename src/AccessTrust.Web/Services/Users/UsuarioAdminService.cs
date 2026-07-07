using AccessTrust.Web.Data;
using AccessTrust.Web.Models;
using MongoDB.Driver;

namespace AccessTrust.Web.Services.Users;

public class UsuarioAdminService : IUsuarioAdminService
{
    private readonly IMongoCollection<Usuario> _usuarios;

    public UsuarioAdminService(IMongoDatabase database)
    {
        _usuarios = database.GetCollection<Usuario>(MongoCollections.Usuarios);
    }

    public async Task<List<Usuario>> ListarUsuariosAsync()
    {
        return await _usuarios
            .Find(Builders<Usuario>.Filter.Empty)
            .SortBy(u => u.Correo)
            .ToListAsync();
    }
}