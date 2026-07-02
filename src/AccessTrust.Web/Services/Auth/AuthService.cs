using AccessTrust.Web.Services.Audit;
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
    private readonly IAuditService _auditService;

    public AuthService(IMongoDatabase database, IAuditService auditService)
    {
        _usuarios = database.GetCollection<Usuario>(MongoCollections.Usuarios);
        _passwordHasher = new PasswordHasher<Usuario>();
        _auditService = auditService;
    }

    public async Task<AuthResult> LoginAsync(string correo, string password)
    {
        var usuario = await _usuarios
            .Find(u => u.Correo == correo)
            .FirstOrDefaultAsync();

        if (usuario is null)
        {
            await _auditService.RegistrarEventoAsync(
                accion: "LOGIN_FALLIDO",
                entidadTipo: "Usuario",
                resultado: ResultadoAuditoria.Fallido,
                detalle: new Dictionary<string, string>
                {
                    { "correo", correo },
                    { "motivo", "usuario_no_encontrado" }
                }
            );

            return AuthResult.Fail("Correo o contraseña incorrectos.");
        }

        if (usuario.Estado == EstadoCuenta.Inactivo)
        {
            await _auditService.RegistrarEventoAsync(
                accion: "LOGIN_FALLIDO",
                entidadTipo: "Usuario",
                resultado: ResultadoAuditoria.Fallido,
                actorUserId: usuario.Id,
                entidadId: usuario.Id,
                detalle: new Dictionary<string, string>
                {
                    { "correo", correo },
                    { "motivo", "cuenta_inactiva" }
                }
            );

            return AuthResult.Fail("La cuenta se encuentra inactiva.");
        }

        if (usuario.Estado == EstadoCuenta.Bloqueado || usuario.LockedUntil.HasValue)
        {
            if (usuario.LockedUntil.HasValue && usuario.LockedUntil.Value <= DateTime.UtcNow)
            {
                await DesbloquearCuentaAsync(usuario.Id!);

                usuario.Estado = EstadoCuenta.Activo;
                usuario.LockedUntil = null;
                usuario.FailedLoginCount = 0;
            }
            else
            {
                await _auditService.RegistrarEventoAsync(
                    accion: "LOGIN_BLOQUEADO",
                    entidadTipo: "Usuario",
                    resultado: ResultadoAuditoria.Denegado,
                    actorUserId: usuario.Id,
                    entidadId: usuario.Id,
                    detalle: new Dictionary<string, string>
                    {
                        { "correo", correo },
                        { "motivo", "cuenta_bloqueada" },
                        { "locked_until", usuario.LockedUntil?.ToString("O") ?? "sin_fecha_definida" }
                    }
                );

                return AuthResult.Fail(
                    usuario.LockedUntil.HasValue
                        ? $"La cuenta está bloqueada temporalmente hasta {usuario.LockedUntil.Value:yyyy-MM-dd HH:mm:ss} UTC."
                        : "La cuenta se encuentra bloqueada."
                );
            }
        }

        var resultadoPassword = _passwordHasher.VerifyHashedPassword(
            usuario,
            usuario.PasswordHash,
            password
        );

        if (resultadoPassword == PasswordVerificationResult.Failed)
        {
            var nuevoConteo = usuario.FailedLoginCount + 1;

            await RegistrarIntentoFallidoAsync(usuario);

            await _auditService.RegistrarEventoAsync(
                accion: "LOGIN_FALLIDO",
                entidadTipo: "Usuario",
                resultado: ResultadoAuditoria.Fallido,
                actorUserId: usuario.Id,
                entidadId: usuario.Id,
                detalle: new Dictionary<string, string>
                {
                    { "correo", correo },
                    { "motivo", "password_incorrecto" },
                    { "intentos_fallidos", nuevoConteo.ToString() }
                }
            );

            if (nuevoConteo >= MaxIntentosFallidos)
            {
                await _auditService.RegistrarEventoAsync(
                    accion: "LOGIN_BLOQUEADO",
                    entidadTipo: "Usuario",
                    resultado: ResultadoAuditoria.Denegado,
                    actorUserId: usuario.Id,
                    entidadId: usuario.Id,
                    detalle: new Dictionary<string, string>
                    {
                        { "correo", correo },
                        { "motivo", "max_intentos_fallidos" },
                        { "intentos_fallidos", nuevoConteo.ToString() },
                        { "bloqueo_minutos", TiempoBloqueo.TotalMinutes.ToString() }
                    }
                );
            }

            return AuthResult.Fail("Correo o contraseña incorrectos.");
        }

        await ReiniciarIntentosFallidosAsync(usuario.Id!);

        usuario.FailedLoginCount = 0;
        usuario.LockedUntil = null;
        usuario.Estado = EstadoCuenta.Activo;

        await _auditService.RegistrarEventoAsync(
            accion: "LOGIN_EXITOSO",
            entidadTipo: "Usuario",
            resultado: ResultadoAuditoria.Exitoso,
            actorUserId: usuario.Id,
            entidadId: usuario.Id,
            detalle: new Dictionary<string, string>
            {
                { "correo", correo },
                { "roles", string.Join(",", usuario.Roles) }
            }
        );

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