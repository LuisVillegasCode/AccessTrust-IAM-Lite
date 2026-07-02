using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AccessTrust.Web.Data;
using AccessTrust.Web.Models;
using MongoDB.Driver;

namespace AccessTrust.Web.Services.Audit;

public class AuditService : IAuditService
{
    private const string GenesisHash = "GENESIS";

    private readonly IMongoCollection<EventoAuditoria> _eventos;
    private readonly string _hmacSecret;

    public AuditService(IMongoDatabase database)
    {
        _eventos = database.GetCollection<EventoAuditoria>(MongoCollections.EventosAuditoria);

        _hmacSecret = Environment.GetEnvironmentVariable("ACCESS_TRUST_AUDIT_HMAC_SECRET")
            ?? throw new InvalidOperationException(
                "No se encontró la variable de entorno ACCESS_TRUST_AUDIT_HMAC_SECRET.");
    }

    public async Task RegistrarEventoAsync(
        string accion,
        string entidadTipo,
        ResultadoAuditoria resultado,
        string? actorUserId = null,
        string? entidadId = null,
        Dictionary<string, string>? detalle = null)
    {
        var ultimoEvento = await _eventos
            .Find(Builders<EventoAuditoria>.Filter.Empty)
            .SortByDescending(e => e.Seq)
            .FirstOrDefaultAsync();

        var seq = ultimoEvento is null ? 1 : ultimoEvento.Seq + 1;
        var prevHash = ultimoEvento?.Hash ?? GenesisHash;

        var evento = new EventoAuditoria
        {
            Seq = seq,
            ActorUserId = actorUserId,
            Accion = accion,
            EntidadTipo = entidadTipo,
            EntidadId = entidadId,
            Resultado = resultado,
            Detalle = detalle ?? new Dictionary<string, string>(),
            PrevHash = prevHash,
            CreatedAt = DateTime.UtcNow
        };

        evento.Hash = CalcularHash(evento);

        await _eventos.InsertOneAsync(evento);
    }

    private string CalcularHash(EventoAuditoria evento)
    {
        var detalleOrdenado = new SortedDictionary<string, string>(evento.Detalle);

        var detalleJson = JsonSerializer.Serialize(detalleOrdenado);

        var contenido = string.Join("|",
            evento.Seq,
            evento.ActorUserId ?? string.Empty,
            evento.Accion,
            evento.EntidadTipo,
            evento.EntidadId ?? string.Empty,
            evento.Resultado.ToString(),
            detalleJson,
            evento.CreatedAt.ToString("O"),
            evento.PrevHash
        );

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_hmacSecret));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(contenido));

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}