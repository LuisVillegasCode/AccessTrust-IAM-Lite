using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AccessTrust.Web.Data;
using AccessTrust.Web.Models;
using MongoDB.Bson;
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
        Dictionary<string, string>? detalle = null,
        IClientSessionHandle? session = null)
    {
        var filtro = Builders<EventoAuditoria>.Filter.Empty;

        var ultimoEvento = session is null
            ? await _eventos
                .Find(filtro)
                .SortByDescending(e => e.Seq)
                .FirstOrDefaultAsync()
            : await _eventos
                .Find(session, filtro)
                .SortByDescending(e => e.Seq)
                .FirstOrDefaultAsync();

        var seq = ultimoEvento is null ? 1 : ultimoEvento.Seq + 1;
        var prevHash = ultimoEvento?.Hash ?? GenesisHash;

        var createdAt = NormalizarFechaUtcParaMongo(DateTime.UtcNow);

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
            CreatedAt = createdAt
        };

        evento.Hash = CalcularHash(evento);

        if (session is null)
        {
            await _eventos.InsertOneAsync(evento);
        }
        else
        {
            await _eventos.InsertOneAsync(session, evento);
        }
    }

    private string CalcularHash(EventoAuditoria evento)
    {
        var detalleOrdenado = new SortedDictionary<string, string>(evento.Detalle);
        var detalleJson = JsonSerializer.Serialize(detalleOrdenado);

        var createdAtNormalizado = NormalizarFechaUtcParaMongo(evento.CreatedAt);

        var contenido = string.Join("|",
            evento.Seq,
            evento.ActorUserId ?? string.Empty,
            evento.Accion,
            evento.EntidadTipo,
            evento.EntidadId ?? string.Empty,
            evento.Resultado.ToString(),
            detalleJson,
            createdAtNormalizado.ToString("O"),
            evento.PrevHash
        );

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_hmacSecret));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(contenido));

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public async Task<List<EventoAuditoria>> ListarEventosAsync(AuditoriaFiltro filtro)
    {
        filtro ??= new AuditoriaFiltro();

        var filters = new List<FilterDefinition<EventoAuditoria>>();
        var builder = Builders<EventoAuditoria>.Filter;

        if (!string.IsNullOrWhiteSpace(filtro.Accion))
        {
            filters.Add(builder.Eq(e => e.Accion, filtro.Accion.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Resultado))
        {
            if (!Enum.TryParse<ResultadoAuditoria>(
                    filtro.Resultado.Trim(),
                    ignoreCase: true,
                    out var resultadoFiltro))
            {
                return new List<EventoAuditoria>();
            }

            filters.Add(builder.Eq(e => e.Resultado, resultadoFiltro));
        }

        if (!string.IsNullOrWhiteSpace(filtro.ActorUserId))
        {
            if (!ObjectId.TryParse(filtro.ActorUserId, out _))
            {
                return new List<EventoAuditoria>();
            }

            filters.Add(builder.Eq(e => e.ActorUserId, filtro.ActorUserId.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(filtro.EntidadTipo))
        {
            filters.Add(builder.Eq(e => e.EntidadTipo, filtro.EntidadTipo.Trim()));
        }

        if (filtro.FechaDesde.HasValue)
        {
            filters.Add(builder.Gte(e => e.CreatedAt, filtro.FechaDesde.Value));
        }

        if (filtro.FechaHasta.HasValue)
        {
            filters.Add(builder.Lte(e => e.CreatedAt, filtro.FechaHasta.Value));
        }

        var finalFilter = filters.Count == 0
            ? builder.Empty
            : builder.And(filters);

        var limite = filtro.Limite <= 0
            ? 100
            : Math.Min(filtro.Limite, 500);

        return await _eventos
            .Find(finalFilter)
            .SortByDescending(e => e.Seq)
            .Limit(limite)
            .ToListAsync();
    }

    public async Task<EventoAuditoria?> GetByIdAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return null;
        }

        return await _eventos
            .Find(e => e.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<IntegridadAuditoriaResult> VerificarIntegridadAsync()
    {
        var eventos = await _eventos
            .Find(Builders<EventoAuditoria>.Filter.Empty)
            .SortBy(e => e.Seq)
            .ToListAsync();

        var resultado = new IntegridadAuditoriaResult
        {
            VerificadoAt = DateTime.UtcNow,
            TotalEventosVerificados = eventos.Count
        };

        string? hashAnteriorEsperado = null;

        foreach (var evento in eventos)
        {
            var esPrimerEvento = hashAnteriorEsperado is null;

            var prevHashValido = esPrimerEvento
                ? string.IsNullOrWhiteSpace(evento.PrevHash) || evento.PrevHash == GenesisHash
                : string.Equals(evento.PrevHash, hashAnteriorEsperado, StringComparison.OrdinalIgnoreCase);

            var hashRecalculado = CalcularHash(evento);

            var hashValido = string.Equals(
                evento.Hash,
                hashRecalculado,
                StringComparison.OrdinalIgnoreCase
            );

            var mensaje = ConstruirMensajeIntegridad(
                prevHashValido,
                hashValido
            );

            resultado.Eventos.Add(new IntegridadEventoResult
            {
                EventoId = evento.Id ?? string.Empty,
                Seq = evento.Seq,
                Accion = evento.Accion,
                CreatedAt = evento.CreatedAt,
                PrevHashValido = prevHashValido,
                HashValido = hashValido,
                Mensaje = mensaje
            });

            hashAnteriorEsperado = evento.Hash;
        }

        resultado.TotalEventosInvalidos = resultado.Eventos.Count(e => !e.EsValido);
        resultado.CadenaIntegra = resultado.TotalEventosInvalidos == 0;

        return resultado;
    }

    private static DateTime NormalizarFechaUtcParaMongo(DateTime fecha)
    {
        var fechaUtc = fecha.Kind == DateTimeKind.Utc
            ? fecha
            : fecha.ToUniversalTime();

        var ticksNormalizados = fechaUtc.Ticks - (fechaUtc.Ticks % TimeSpan.TicksPerMillisecond);

        return new DateTime(ticksNormalizados, DateTimeKind.Utc);
    }
    
    private static string ConstruirMensajeIntegridad(
        bool prevHashValido,
        bool hashValido)
    {
        if (prevHashValido && hashValido)
        {
            return "Evento íntegro.";
        }

        if (!prevHashValido && !hashValido)
        {
            return "El enlace con el evento anterior y el hash del evento son inválidos.";
        }

        if (!prevHashValido)
        {
            return "El PrevHash no coincide con el hash del evento anterior.";
        }

        return "El hash del evento no coincide con el contenido almacenado.";
    }
}