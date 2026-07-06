using System.Security.Cryptography;
using System.Text;
using AccessTrust.Web.Data;
using AccessTrust.Web.Models;
using AccessTrust.Web.Services.Audit;
using AccessTrust.Web.Services.Resources;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AccessTrust.Web.Services.ExternalAccess;

public class ExternalAccessTicketService : IExternalAccessTicketService
{
    private const int TicketDurationMinutes = 2;

    private readonly IMongoCollection<TicketAccesoExterno> _tickets;
    private readonly IMongoCollection<CredencialTemporal> _credenciales;
    private readonly IMongoClient _mongoClient;
    private readonly IRecursoService _recursoService;
    private readonly IAuditService _auditService;

    public ExternalAccessTicketService(
        IMongoDatabase database,
        IMongoClient mongoClient,
        IRecursoService recursoService,
        IAuditService auditService)
    {
        _tickets = database.GetCollection<TicketAccesoExterno>(
            MongoCollections.TicketsAccesoExterno
        );

        _credenciales = database.GetCollection<CredencialTemporal>(
            MongoCollections.CredencialesTemporales
        );

        _mongoClient = mongoClient;
        _recursoService = recursoService;
        _auditService = auditService;
    }

    public async Task<CrearTicketAccesoExternoResult> CrearAsync(
        string usuarioId,
        string credencialId,
        string? createdByIp,
        string? userAgent)
    {
        if (!ObjectId.TryParse(usuarioId, out _))
        {
            return CrearTicketAccesoExternoResult.Fail("El usuario no tiene un identificador válido.");
        }

        if (!ObjectId.TryParse(credencialId, out _))
        {
            return CrearTicketAccesoExternoResult.Fail("La credencial no tiene un identificador válido.");
        }

        var credencial = await _credenciales
            .Find(c => c.Id == credencialId)
            .FirstOrDefaultAsync();

        if (credencial is null)
        {
            return CrearTicketAccesoExternoResult.Fail("La credencial temporal no existe.");
        }

        if (credencial.UsuarioId != usuarioId)
        {
            await RegistrarAuditoriaAsync(
                "TICKET_EXTERNO_DENEGADO",
                ResultadoAuditoria.Denegado,
                usuarioId,
                "CredencialTemporal",
                credencialId,
                new Dictionary<string, string>
                {
                    { "motivo", "La credencial no pertenece al usuario solicitante." },
                    { "recurso_id", credencial.RecursoId }
                }
            );

            return CrearTicketAccesoExternoResult.Fail("La credencial no pertenece al usuario solicitante.");
        }

        if (credencial.Estado != EstadoCredencial.Activa)
        {
            return CrearTicketAccesoExternoResult.Fail(
                $"La credencial temporal no está activa. Estado actual: {credencial.Estado}."
            );
        }

        if (credencial.ExpiresAt <= DateTime.UtcNow)
        {
            await MarcarCredencialComoExpiradaAsync(credencial);

            return CrearTicketAccesoExternoResult.Fail("La credencial temporal ha expirado.");
        }

        if (credencial.UsosRealizados >= credencial.MaxUsos)
        {
            await MarcarCredencialComoUsadaAsync(credencial);

            return CrearTicketAccesoExternoResult.Fail(
                "La credencial temporal alcanzó el máximo de usos permitidos."
            );
        }

        var recurso = await _recursoService.GetByIdAsync(credencial.RecursoId);

        if (recurso is null)
        {
            return CrearTicketAccesoExternoResult.Fail("El recurso asociado a la credencial no existe.");
        }

        if (!recurso.Activo)
        {
            return CrearTicketAccesoExternoResult.Fail("El recurso asociado no se encuentra activo.");
        }

        if (string.IsNullOrWhiteSpace(recurso.UrlExterna))
        {
            return CrearTicketAccesoExternoResult.Fail(
                "El recurso no tiene una URL externa configurada."
            );
        }

        var ticketPlano = GenerarTicketPlano();
        var ticketHash = CalcularSha256(ticketPlano);
        var issuedAt = DateTime.UtcNow;
        var expiresAt = issuedAt.AddMinutes(TicketDurationMinutes);

        var ticket = new TicketAccesoExterno
        {
            UsuarioId = usuarioId,
            CredencialId = credencialId,
            RecursoId = credencial.RecursoId,
            TicketHash = ticketHash,
            UrlExterna = recurso.UrlExterna,
            Estado = EstadoTicketAccesoExterno.Activo,
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt,
            UsedAt = null,
            RevokedAt = null,
            CreatedByIp = createdByIp,
            UserAgent = userAgent
        };

        await _tickets.InsertOneAsync(ticket);

        await RegistrarAuditoriaAsync(
            "TICKET_EXTERNO_EMITIDO",
            ResultadoAuditoria.Exitoso,
            usuarioId,
            "TicketAccesoExterno",
            ticket.Id,
            new Dictionary<string, string>
            {
                { "credencial_id", credencialId },
                { "recurso_id", credencial.RecursoId },
                { "url_externa", recurso.UrlExterna },
                { "expires_at", expiresAt.ToString("O") }
            }
        );

        return CrearTicketAccesoExternoResult.Ok(
            ticketPlano,
            ticket.Id ?? string.Empty,
            credencialId,
            credencial.RecursoId,
            recurso.UrlExterna,
            expiresAt
        );
    }

    public async Task<ValidarTicketAccesoExternoResult> ValidarAsync(
        string recursoId,
        string ticketPlano)
    {
        if (!ObjectId.TryParse(recursoId, out _))
        {
            return ValidarTicketAccesoExternoResult.Fail(
                "El recurso solicitado no es válido.",
                "RECURSO_INVALIDO"
            );
        }

        if (string.IsNullOrWhiteSpace(ticketPlano))
        {
            await RegistrarAuditoriaAsync(
                "TICKET_EXTERNO_VACIO",
                ResultadoAuditoria.Denegado,
                null,
                "TicketAccesoExterno",
                null,
                new Dictionary<string, string>
                {
                    { "recurso_id", recursoId },
                    { "motivo", "No se recibió ticket externo." }
                }
            );

            return ValidarTicketAccesoExternoResult.Fail(
                "No se recibió ticket externo.",
                "TICKET_EXTERNO_VACIO",
                recursoId
            );
        }

        var ticketHash = CalcularSha256(ticketPlano.Trim());

        var ticket = await _tickets
            .Find(t => t.TicketHash == ticketHash)
            .FirstOrDefaultAsync();

        if (ticket is null)
        {
            await RegistrarAuditoriaAsync(
                "TICKET_EXTERNO_INVALIDO",
                ResultadoAuditoria.Denegado,
                null,
                "TicketAccesoExterno",
                null,
                new Dictionary<string, string>
                {
                    { "recurso_id", recursoId },
                    { "motivo", "Ticket externo no encontrado." }
                }
            );

            return ValidarTicketAccesoExternoResult.Fail(
                "Ticket externo no encontrado o inválido.",
                "TICKET_EXTERNO_INVALIDO",
                recursoId
            );
        }

        if (ticket.RecursoId != recursoId)
        {
            await RegistrarAuditoriaAsync(
                "TICKET_RECURSO_INVALIDO",
                ResultadoAuditoria.Denegado,
                ticket.UsuarioId,
                "TicketAccesoExterno",
                ticket.Id,
                new Dictionary<string, string>
                {
                    { "ticket_recurso_id", ticket.RecursoId },
                    { "recurso_solicitado_id", recursoId }
                }
            );

            return ValidarTicketAccesoExternoResult.Fail(
                "El ticket externo no corresponde al recurso solicitado.",
                "TICKET_RECURSO_INVALIDO",
                recursoId
            );
        }

        if (ticket.Estado != EstadoTicketAccesoExterno.Activo)
        {
            await RegistrarAuditoriaAsync(
                "TICKET_EXTERNO_NO_ACTIVO",
                ResultadoAuditoria.Denegado,
                ticket.UsuarioId,
                "TicketAccesoExterno",
                ticket.Id,
                new Dictionary<string, string>
                {
                    { "estado_ticket", ticket.Estado.ToString() },
                    { "recurso_id", recursoId }
                }
            );

            return ValidarTicketAccesoExternoResult.Fail(
                $"El ticket externo no está activo. Estado actual: {ticket.Estado}.",
                "TICKET_EXTERNO_NO_ACTIVO",
                recursoId
            );
        }

        if (ticket.ExpiresAt <= DateTime.UtcNow)
        {
            await MarcarTicketComoExpiradoAsync(ticket);

            await RegistrarAuditoriaAsync(
                "TICKET_EXTERNO_EXPIRADO",
                ResultadoAuditoria.Denegado,
                ticket.UsuarioId,
                "TicketAccesoExterno",
                ticket.Id,
                new Dictionary<string, string>
                {
                    { "recurso_id", recursoId },
                    { "expires_at", ticket.ExpiresAt.ToString("O") }
                }
            );

            return ValidarTicketAccesoExternoResult.Fail(
                "El ticket externo ha expirado.",
                "TICKET_EXTERNO_EXPIRADO",
                recursoId
            );
        }

        var credencial = await _credenciales
            .Find(c => c.Id == ticket.CredencialId)
            .FirstOrDefaultAsync();

        if (credencial is null)
        {
            return ValidarTicketAccesoExternoResult.Fail(
                "La credencial asociada al ticket no existe.",
                "CREDENCIAL_NO_ENCONTRADA",
                recursoId
            );
        }

        if (credencial.Estado != EstadoCredencial.Activa)
        {
            return ValidarTicketAccesoExternoResult.Fail(
                $"La credencial temporal no está activa. Estado actual: {credencial.Estado}.",
                "CREDENCIAL_NO_ACTIVA",
                recursoId
            );
        }

        if (credencial.ExpiresAt <= DateTime.UtcNow)
        {
            await MarcarCredencialComoExpiradaAsync(credencial);

            return ValidarTicketAccesoExternoResult.Fail(
                "La credencial temporal ha expirado.",
                "CREDENCIAL_EXPIRADA",
                recursoId
            );
        }

        if (credencial.UsosRealizados >= credencial.MaxUsos)
        {
            await MarcarCredencialComoUsadaAsync(credencial);

            return ValidarTicketAccesoExternoResult.Fail(
                "La credencial temporal alcanzó el máximo de usos permitidos.",
                "MAX_USOS_ALCANZADO",
                recursoId
            );
        }

        var recurso = await _recursoService.GetByIdAsync(recursoId);

        if (recurso is null)
        {
            return ValidarTicketAccesoExternoResult.Fail(
                "El recurso solicitado no existe.",
                "RECURSO_NO_ENCONTRADO",
                recursoId
            );
        }

        var nuevoNumeroUsos = credencial.UsosRealizados + 1;
        var nuevoEstadoCredencial = nuevoNumeroUsos >= credencial.MaxUsos
            ? EstadoCredencial.Usada
            : EstadoCredencial.Activa;

        using var session = await _mongoClient.StartSessionAsync();

        try
        {
            session.StartTransaction();

            var filtroCredencial = Builders<CredencialTemporal>.Filter.And(
                Builders<CredencialTemporal>.Filter.Eq(c => c.Id, credencial.Id),
                Builders<CredencialTemporal>.Filter.Eq(c => c.Estado, EstadoCredencial.Activa),
                Builders<CredencialTemporal>.Filter.Gt(c => c.ExpiresAt, DateTime.UtcNow),
                Builders<CredencialTemporal>.Filter.Lt(c => c.UsosRealizados, credencial.MaxUsos)
            );

            var updateCredencial = Builders<CredencialTemporal>.Update
                .Inc(c => c.UsosRealizados, 1)
                .Set(c => c.Estado, nuevoEstadoCredencial);

            var credencialUpdateResult = await _credenciales.UpdateOneAsync(
                session,
                filtroCredencial,
                updateCredencial
            );

            if (credencialUpdateResult.ModifiedCount == 0)
            {
                await session.AbortTransactionAsync();

                return ValidarTicketAccesoExternoResult.Fail(
                    "No se pudo consumir la credencial temporal.",
                    "ERROR_CONSUMIR_CREDENCIAL",
                    recursoId
                );
            }

            var filtroTicket = Builders<TicketAccesoExterno>.Filter.And(
                Builders<TicketAccesoExterno>.Filter.Eq(t => t.Id, ticket.Id),
                Builders<TicketAccesoExterno>.Filter.Eq(t => t.Estado, EstadoTicketAccesoExterno.Activo),
                Builders<TicketAccesoExterno>.Filter.Gt(t => t.ExpiresAt, DateTime.UtcNow)
            );

            var updateTicket = Builders<TicketAccesoExterno>.Update
                .Set(t => t.Estado, EstadoTicketAccesoExterno.Usado)
                .Set(t => t.UsedAt, DateTime.UtcNow);

            var ticketUpdateResult = await _tickets.UpdateOneAsync(
                session,
                filtroTicket,
                updateTicket
            );

            if (ticketUpdateResult.ModifiedCount == 0)
            {
                await session.AbortTransactionAsync();

                return ValidarTicketAccesoExternoResult.Fail(
                    "No se pudo consumir el ticket externo.",
                    "ERROR_CONSUMIR_TICKET",
                    recursoId
                );
            }

            await _auditService.RegistrarEventoAsync(
                accion: "ACCESO_EXTERNO_PERMITIDO",
                entidadTipo: "TicketAccesoExterno",
                resultado: ResultadoAuditoria.Permitido,
                actorUserId: ticket.UsuarioId,
                entidadId: ticket.Id,
                detalle: new Dictionary<string, string>
                {
                    { "credencial_id", credencial.Id ?? string.Empty },
                    { "recurso_id", recursoId },
                    { "recurso_nombre", recurso.Nombre },
                    { "usos_realizados", nuevoNumeroUsos.ToString() },
                    { "max_usos", credencial.MaxUsos.ToString() }
                },
                session: session
            );

            await session.CommitTransactionAsync();

            return ValidarTicketAccesoExternoResult.Ok(
                recursoId,
                recurso.Nombre,
                recurso.Tipo,
                recurso.Sensibilidad.ToString(),
                credencial.Id ?? string.Empty,
                credencial.ExpiresAt,
                nuevoNumeroUsos,
                credencial.MaxUsos
            );
        }
        catch
        {
            await session.AbortTransactionAsync();

            return ValidarTicketAccesoExternoResult.Fail(
                "Ocurrió un error al validar el ticket externo.",
                "ERROR_VALIDAR_TICKET",
                recursoId
            );
        }
    }

    private async Task MarcarTicketComoExpiradoAsync(TicketAccesoExterno ticket)
    {
        if (string.IsNullOrWhiteSpace(ticket.Id))
        {
            return;
        }

        var filtro = Builders<TicketAccesoExterno>.Filter.And(
            Builders<TicketAccesoExterno>.Filter.Eq(t => t.Id, ticket.Id),
            Builders<TicketAccesoExterno>.Filter.Eq(t => t.Estado, EstadoTicketAccesoExterno.Activo),
            Builders<TicketAccesoExterno>.Filter.Lte(t => t.ExpiresAt, DateTime.UtcNow)
        );

        var update = Builders<TicketAccesoExterno>.Update
            .Set(t => t.Estado, EstadoTicketAccesoExterno.Expirado);

        await _tickets.UpdateOneAsync(filtro, update);
    }

    private async Task MarcarCredencialComoExpiradaAsync(CredencialTemporal credencial)
    {
        if (string.IsNullOrWhiteSpace(credencial.Id))
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

        await _credenciales.UpdateOneAsync(filtro, update);
    }

    private async Task MarcarCredencialComoUsadaAsync(CredencialTemporal credencial)
    {
        if (string.IsNullOrWhiteSpace(credencial.Id))
        {
            return;
        }

        var filtro = Builders<CredencialTemporal>.Filter.And(
            Builders<CredencialTemporal>.Filter.Eq(c => c.Id, credencial.Id),
            Builders<CredencialTemporal>.Filter.Eq(c => c.Estado, EstadoCredencial.Activa),
            Builders<CredencialTemporal>.Filter.Gte(c => c.UsosRealizados, credencial.MaxUsos)
        );

        var update = Builders<CredencialTemporal>.Update
            .Set(c => c.Estado, EstadoCredencial.Usada);

        await _credenciales.UpdateOneAsync(filtro, update);
    }

    private async Task RegistrarAuditoriaAsync(
        string accion,
        ResultadoAuditoria resultado,
        string? actorUserId,
        string entidadTipo,
        string? entidadId,
        Dictionary<string, string> detalle)
    {
        await _auditService.RegistrarEventoAsync(
            accion: accion,
            entidadTipo: entidadTipo,
            resultado: resultado,
            actorUserId: actorUserId,
            entidadId: entidadId,
            detalle: detalle
        );
    }

    private static string GenerarTicketPlano()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string CalcularSha256(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}