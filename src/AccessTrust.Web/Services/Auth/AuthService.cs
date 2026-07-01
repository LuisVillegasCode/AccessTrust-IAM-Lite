using AccessTrust.Web.Data;
using AccessTrust.Web.Models;
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;

namespace AccessTrust.Web.Services.Auth;

public class AuthService : IAuthService
{
    private const int MaxIntentosFallidos = 5;
    private static readonly TimeSpan TiempoBloqueo = TimeSpan.FromMinutes(15);

    private readonly IMongoCollection<Usuario> _usuarios;
    private readonly PasswordHasher<Usuario> _passwordHasher;

    public AuthService(IMongoDatabase database)
    {
        _usuarios = database.GetCollection<Usuario>(MongoCollections.Usuarios);
        _passwordHasher = new PasswordHasher<Usuario>();
    }

    public async Task<AuthResult> LoginAsync(string correo, string password)
    {
        var usuario = await _usuarios
            .Find(u => u.Correo == correo)
            .FirstOrDefaultAsync();

        if (usuario is null)
        {
            return AuthResult.Fail("Correo o contraseña incorrectos.");
        }

        if (usuario.Estado == EstadoCuenta.Inactivo)
        {
            return AuthResult.Fail("La cuenta se encuentra inactiva.");
        }

        if (usuario.LockedUntil.HasValue)
        {
            if (usuario.LockedUntil.Value > DateTime.UtcNow)
            {
                return AuthResult.Fail(
                    $"La cuenta está bloqueada temporalmente hasta {usuario.LockedUntil.Value:yyyy-MM-dd HH:mm:ss} UTC."
                );
            }

            await DesbloquearCuentaAsync(usuario.Id!);
            usuario.Estado = EstadoCuenta.Activo;
            usuario.LockedUntil = null;
            usuario.FailedLoginCount = 0;
        }

        var resultadoPassword = _passwordHasher.VerifyHashedPassword(
            usuario,
            usuario.PasswordHash,
            password
        );

        if (resultadoPassword == PasswordVerificationResult.Failed)
        {
            await RegistrarIntentoFallidoAsync(usuario);
            return AuthResult.Fail("Correo o contraseña incorrectos.");
        }

        await ReiniciarIntentosFallidosAsync(usuario.Id!);

        usuario.FailedLoginCount = 0;
        usuario.LockedUntil = null;
        usuario.Estado = EstadoCuenta.Activo;

        return AuthResult.Ok(usuario);
    }

    private async Task RegistrarIntentoFallidoAsync(Usuario usuario)
    {
        var nuevoConteo = usuario.FailedLoginCount + 1;

        var update = Builders<Usuario>.Update
            .Set(u => u.FailedLoginCount, nuevoConteo);

        if (nuevoConteo >= MaxIntentosFallidos)
        {
            update = update
                .Set(u => u.Estado, EstadoCuenta.Bloqueado)
                .Set(u => u.LockedUntil, DateTime.UtcNow.Add(TiempoBloqueo));
        }

        await _usuarios.UpdateOneAsync(
            u => u.Id == usuario.Id,
            update
        );
    }

    private async Task ReiniciarIntentosFallidosAsync(string usuarioId)
    {
        var update = Builders<Usuario>.Update
            .Set(u => u.FailedLoginCount, 0)
            .Set(u => u.LockedUntil, null)
            .Set(u => u.Estado, EstadoCuenta.Activo);

        await _usuarios.UpdateOneAsync(
            u => u.Id == usuarioId,
            update
        );
    }

    private async Task DesbloquearCuentaAsync(string usuarioId)
    {
        var update = Builders<Usuario>.Update
            .Set(u => u.Estado, EstadoCuenta.Activo)
            .Set(u => u.FailedLoginCount, 0)
            .Set(u => u.LockedUntil, null);

        await _usuarios.UpdateOneAsync(
            u => u.Id == usuarioId,
            update
        );
    }
}