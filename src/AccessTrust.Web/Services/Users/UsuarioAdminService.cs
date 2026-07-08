using AccessTrust.Web.Data;
using AccessTrust.Web.Models;
using AccessTrust.Web.Services.Audit;
using AccessTrust.Web.Services.Security;
using AccessTrust.Web.ViewModels.Users;
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;

namespace AccessTrust.Web.Services.Users;

public class UsuarioAdminService : IUsuarioAdminService
{
    private readonly IMongoCollection<Usuario> _usuarios;
    private readonly IFieldEncryptionService _fieldEncryptionService;
    private readonly IAuditService _auditService;
    private readonly PasswordHasher<Usuario> _passwordHasher = new();

    public UsuarioAdminService(
        IMongoDatabase database,
        IFieldEncryptionService fieldEncryptionService,
        IAuditService auditService)
    {
        _usuarios = database.GetCollection<Usuario>(MongoCollections.Usuarios);
        _fieldEncryptionService = fieldEncryptionService;
        _auditService = auditService;
    }

    public async Task<List<Usuario>> ListarUsuariosAsync()
    {
        return await _usuarios
            .Find(Builders<Usuario>.Filter.Empty)
            .SortBy(u => u.Correo)
            .ToListAsync();
    }

    public async Task<(bool Success, string Message)> CrearUsuarioAsync(
        CrearUsuarioAdminViewModel model,
        string actorUserId)
    {
        var correoNormalizado = model.Correo.Trim().ToLowerInvariant();

        var rolesSeleccionados = model.ObtenerRolesSeleccionados();

        var rolesPermitidos = new[] { "Administrador", "Aprobador", "Solicitante" };

        if (!rolesSeleccionados.Any())
        {
            return (false, "Debe seleccionar al menos un rol.");
        }

        if (rolesSeleccionados.Any(r => !rolesPermitidos.Contains(r)))
        {
            return (false, "Se intentó asignar un rol no permitido.");
        }

        var existeCorreo = await _usuarios
            .Find(u => u.Correo == correoNormalizado)
            .AnyAsync();

        if (existeCorreo)
        {
            return (false, "Ya existe un usuario registrado con ese correo.");
        }

        var usuario = new Usuario
        {
            Nombre = model.Nombre.Trim(),
            Correo = correoNormalizado,
            Roles = rolesSeleccionados,
            Estado = model.Estado,
            FailedLoginCount = 0,
            LockedUntil = null,
            CreatedAt = DateTime.UtcNow
        };

        usuario.PasswordHash = _passwordHasher.HashPassword(
            usuario,
            model.PasswordTemporal
        );

        usuario.DocumentoIdentidadCifrado = _fieldEncryptionService.EncryptToBase64(
            model.DocumentoIdentidad.Trim()
        );

        await _usuarios.InsertOneAsync(usuario);

        await _auditService.RegistrarEventoAsync(
            accion: "USUARIO_CREADO",
            entidadTipo: "Usuario",
            resultado: ResultadoAuditoria.Exitoso,
            actorUserId: actorUserId,
            entidadId: usuario.Id,
            detalle: new Dictionary<string, string>
            {
                { "usuario_creado_id", usuario.Id ?? string.Empty },
                { "correo", usuario.Correo },
                { "nombre", usuario.Nombre },
                { "roles", string.Join(",", usuario.Roles) },
                { "estado", usuario.Estado.ToString() }
            }
        );

        return (true, "Usuario creado correctamente.");
    }
}