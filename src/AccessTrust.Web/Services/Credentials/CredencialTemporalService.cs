using System.Security.Cryptography;
using System.Text;
using AccessTrust.Web.Data;
using AccessTrust.Web.Models;
using AccessTrust.Web.Services.Audit;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AccessTrust.Web.Services.Credentials;

public class CredencialTemporalService : ICredencialTemporalService
{
    private readonly IMongoCollection<CredencialTemporal> _credenciales;
    private readonly IAuditService _auditService;

    public CredencialTemporalService(
        IMongoDatabase database,
        IAuditService auditService)
    {
        _credenciales = database.GetCollection<CredencialTemporal>(
            MongoCollections.CredencialesTemporales
        );

        _auditService = auditService;
    }

    public async Task<(bool Success, string Message, CredencialTemporal? Credencial, string? TokenPlano)> EmitirAsync(
        SolicitudAcceso solicitud,
        PoliticaAcceso politica,
        int duracionAprobadaMin,
        IClientSessionHandle? session = null)
    {
        if (string.IsNullOrWhiteSpace(solicitud.Id) || !ObjectId.TryParse(solicitud.Id, out _))
        {
            return (false, "La solicitud no tiene un identificador válido.", null, null);
        }

        if (!ObjectId.TryParse(solicitud.UsuarioId, out _))
        {
            return (false, "El usuario asociado a la solicitud no es válido.", null, null);
        }

        if (!ObjectId.TryParse(solicitud.RecursoId, out _))
        {
            return (false, "El recurso asociado a la solicitud no es válido.", null, null);
        }

        if (duracionAprobadaMin <= 0)
        {
            return (false, "La duración aprobada debe ser mayor que cero.", null, null);
        }

        if (politica.DuracionMaxMin <= 0)
        {
            return (false, "La política asociada no tiene una duración máxima válida.", null, null);
        }

        if (politica.MaxUsos <= 0)
        {
            return (false, "La política asociada no tiene un número máximo de usos válido.", null, null);
        }

        var duracionFinalMin = Math.Min(duracionAprobadaMin, politica.DuracionMaxMin);
        var fechaEmision = DateTime.UtcNow;
        var tokenPlano = GenerarTokenPlano();
        var tokenHash = CalcularSha256(tokenPlano);

        var credencial = new CredencialTemporal
        {
            UsuarioId = solicitud.UsuarioId,
            RecursoId = solicitud.RecursoId,
            SolicitudId = solicitud.Id,
            TokenHash = tokenHash,
            Estado = EstadoCredencial.Activa,
            IssuedAt = fechaEmision,
            ExpiresAt = fechaEmision.AddMinutes(duracionFinalMin),
            MaxUsos = politica.MaxUsos,
            UsosRealizados = 0,
            RevokedAt = null
        };

        if (session is null)
        {
            await _credenciales.InsertOneAsync(credencial);
        }
        else
        {
            await _credenciales.InsertOneAsync(session, credencial);
        }

        return (true, "Credencial temporal emitida correctamente.", credencial, tokenPlano);
    }

    public async Task<CredencialTemporal?> GetByIdAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return null;
        }

        return await _credenciales
            .Find(c => c.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<List<CredencialTemporal>> GetByUsuarioAsync(string usuarioId)
    {
        if (!ObjectId.TryParse(usuarioId, out _))
        {
            return new List<CredencialTemporal>();
        }

        return await _credenciales
            .Find(c => c.UsuarioId == usuarioId)
            .SortByDescending(c => c.IssuedAt)
            .ToListAsync();
    }

    public async Task<ValidacionCredencialResult> ValidarTokenAsync(
        string recursoId,
        string tokenPlano)
    {
        if (!ObjectId.TryParse(recursoId, out _))
        {
            var result = ValidacionCredencialResult.Fail(
                "El recurso solicitado no es válido.",
                "RECURSO_INVALIDO"
            );

            await RegistrarAuditoriaValidacionAsync(
                result.MotivoCodigo,
                ResultadoAuditoria.Denegado,
                recursoId,
                null,
                result.Mensaje
            );

            return result;
        }

        if (string.IsNullOrWhiteSpace(tokenPlano))
        {
            var result = ValidacionCredencialResult.Fail(
                "El token temporal es obligatorio.",
                "TOKEN_VACIO"
            );

            await RegistrarAuditoriaValidacionAsync(
                result.MotivoCodigo,
                ResultadoAuditoria.Denegado,
                recursoId,
                null,
                result.Mensaje
            );

            return result;
        }

        var tokenHash = CalcularSha256(tokenPlano.Trim());

        var credencial = await _credenciales
            .Find(c => c.TokenHash == tokenHash)
            .FirstOrDefaultAsync();

        if (credencial is null)
        {
            var result = ValidacionCredencialResult.Fail(
                "Token temporal no encontrado o inválido.",
                "TOKEN_INVALIDO"
            );

            await RegistrarAuditoriaValidacionAsync(
                result.MotivoCodigo,
                ResultadoAuditoria.Denegado,
                recursoId,
                null,
                result.Mensaje
            );

            return result;
        }

        if (credencial.RecursoId != recursoId)
        {
            var result = ValidacionCredencialResult.Fail(
                "El token temporal no corresponde al recurso solicitado.",
                "TOKEN_RECURSO_INVALIDO",
                credencial
            );

            await RegistrarAuditoriaValidacionAsync(
                result.MotivoCodigo,
                ResultadoAuditoria.Denegado,
                recursoId,
                credencial,
                result.Mensaje
            );

            return result;
        }

        if (credencial.Estado != EstadoCredencial.Activa)
        {
            var result = ValidacionCredencialResult.Fail(
                $"La credencial temporal no está activa. Estado actual: {credencial.Estado}.",
                "CREDENCIAL_NO_ACTIVA",
                credencial
            );

            await RegistrarAuditoriaValidacionAsync(
                result.MotivoCodigo,
                ResultadoAuditoria.Denegado,
                recursoId,
                credencial,
                result.Mensaje
            );

            return result;
        }

        if (credencial.ExpiresAt <= DateTime.UtcNow)
        {
            await MarcarComoExpiradaAsync(credencial);

            var result = ValidacionCredencialResult.Fail(
                "La credencial temporal ha expirado.",
                "TOKEN_EXPIRADO",
                credencial
            );

            await RegistrarAuditoriaValidacionAsync(
                result.MotivoCodigo,
                ResultadoAuditoria.Denegado,
                recursoId,
                credencial,
                result.Mensaje
            );

            return result;
        }

        if (credencial.UsosRealizados >= credencial.MaxUsos)
        {
            var result = ValidacionCredencialResult.Fail(
                "La credencial temporal alcanzó el máximo de usos permitidos.",
                "MAX_USOS_ALCANZADO",
                credencial
            );

            await RegistrarAuditoriaValidacionAsync(
                result.MotivoCodigo,
                ResultadoAuditoria.Denegado,
                recursoId,
                credencial,
                result.Mensaje
            );

            return result;
        }

        var usoActualizado = await RegistrarUsoAsync(credencial);

        if (!usoActualizado.Success)
        {
            var result = ValidacionCredencialResult.Fail(
                usoActualizado.Message,
                "ERROR_ACTUALIZAR_USO",
                credencial
            );

            await RegistrarAuditoriaValidacionAsync(
                result.MotivoCodigo,
                ResultadoAuditoria.Denegado,
                recursoId,
                credencial,
                result.Mensaje
            );

            return result;
        }

        var ok = ValidacionCredencialResult.Ok(credencial);

        await RegistrarAuditoriaValidacionAsync(
            ok.MotivoCodigo,
            ResultadoAuditoria.Permitido,
            recursoId,
            credencial,
            ok.Mensaje
        );

        return ok;
    }

    private async Task MarcarComoExpiradaAsync(CredencialTemporal credencial)
    {
        if (string.IsNullOrWhiteSpace(credencial.Id) || !ObjectId.TryParse(credencial.Id, out _))
        {
            return;
        }

        if (credencial.Estado != EstadoCredencial.Activa)
        {
            return;
        }

        var filtro = Builders<CredencialTemporal>.Filter.And(
            Builders<CredencialTemporal>.Filter.Eq(c => c.Id, credencial.Id),
            Builders<CredencialTemporal>.Filter.Eq(c => c.Estado, EstadoCredencial.Activa),
            Builders<CredencialTemporal>.Filter.Lte(c => c.ExpiresAt, DateTime.UtcNow)
        );

        var update = Builders<CredencialTemporal>.Update
            .Set(c => c.Estado, EstadoCredencial.Expirada);

        var result = await _credenciales.UpdateOneAsync(filtro, update);

        if (result.ModifiedCount > 0)
        {
            credencial.Estado = EstadoCredencial.Expirada;
        }
    }

    private async Task<(bool Success, string Message)> RegistrarUsoAsync(CredencialTemporal credencial)
    {
        if (string.IsNullOrWhiteSpace(credencial.Id) || !ObjectId.TryParse(credencial.Id, out _))
        {
            return (false, "La credencial temporal no tiene un identificador válido.");
        }

        var nuevoNumeroUsos = credencial.UsosRealizados + 1;
        var nuevoEstado = nuevoNumeroUsos >= credencial.MaxUsos
            ? EstadoCredencial.Usada
            : EstadoCredencial.Activa;

        var filtro = Builders<CredencialTemporal>.Filter.And(
            Builders<CredencialTemporal>.Filter.Eq(c => c.Id, credencial.Id),
            Builders<CredencialTemporal>.Filter.Eq(c => c.Estado, EstadoCredencial.Activa),
            Builders<CredencialTemporal>.Filter.Gt(c => c.ExpiresAt, DateTime.UtcNow)
        );

        var update = Builders<CredencialTemporal>.Update
            .Inc(c => c.UsosRealizados, 1)
            .Set(c => c.Estado, nuevoEstado);

        var result = await _credenciales.UpdateOneAsync(filtro, update);

        if (result.ModifiedCount == 0)
        {
            return (false, "No se pudo registrar el uso de la credencial temporal.");
        }

        credencial.UsosRealizados = nuevoNumeroUsos;
        credencial.Estado = nuevoEstado;

        return (true, "Uso de credencial registrado correctamente.");
    }

    private async Task RegistrarAuditoriaValidacionAsync(
        string accion,
        ResultadoAuditoria resultado,
        string recursoId,
        CredencialTemporal? credencial,
        string mensaje)
    {
        var detalle = new Dictionary<string, string>
        {
            { "recurso_id", recursoId },
            { "mensaje", mensaje }
        };

        if (credencial is not null)
        {
            if (!string.IsNullOrWhiteSpace(credencial.Id))
            {
                detalle["credencial_id"] = credencial.Id;
            }

            detalle["usuario_id"] = credencial.UsuarioId;
            detalle["estado_credencial"] = credencial.Estado.ToString();
            detalle["usos_realizados"] = credencial.UsosRealizados.ToString();
            detalle["max_usos"] = credencial.MaxUsos.ToString();
            detalle["expires_at"] = credencial.ExpiresAt.ToString("O");
        }

        await _auditService.RegistrarEventoAsync(
            accion: accion,
            entidadTipo: "CredencialTemporal",
            resultado: resultado,
            actorUserId: credencial?.UsuarioId,
            entidadId: credencial?.Id,
            detalle: detalle
        );
    }

    private static string GenerarTokenPlano()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string CalcularSha256(string valor)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(valor));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}