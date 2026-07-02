using System.Security.Cryptography;
using System.Text;
using AccessTrust.Web.Data;
using AccessTrust.Web.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AccessTrust.Web.Services.Credentials;

public class CredencialTemporalService : ICredencialTemporalService
{
    private readonly IMongoCollection<CredencialTemporal> _credenciales;

    public CredencialTemporalService(IMongoDatabase database)
    {
        _credenciales = database.GetCollection<CredencialTemporal>(
            MongoCollections.CredencialesTemporales
        );
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