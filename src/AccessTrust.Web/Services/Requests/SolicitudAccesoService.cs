using AccessTrust.Web.Services.Credentials;
using AccessTrust.Web.Data;
using AccessTrust.Web.Models;
using AccessTrust.Web.Services.Audit;
using AccessTrust.Web.Services.Policies;
using AccessTrust.Web.Services.Resources;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AccessTrust.Web.Services.Requests;
public class SolicitudAccesoService : ISolicitudAccesoService
{
    private readonly IMongoCollection<SolicitudAcceso> _solicitudes;
    private readonly IMongoCollection<Recurso> _recursos;
    private readonly IRecursoService _recursoService;
    private readonly IPoliticaAccesoService _politicaAccesoService;
    private readonly IAuditService _auditService;
    private readonly IMongoClient _mongoClient;
    private readonly ICredencialTemporalService _credencialTemporalService;

    public SolicitudAccesoService(
        IMongoDatabase database,
        IMongoClient mongoClient,
        IRecursoService recursoService,
        IPoliticaAccesoService politicaAccesoService,
        ICredencialTemporalService credencialTemporalService,
        IAuditService auditService)
    {
        _solicitudes = database.GetCollection<SolicitudAcceso>(MongoCollections.SolicitudesAcceso);
        _recursos = database.GetCollection<Recurso>(MongoCollections.Recursos);
        _mongoClient = mongoClient;
        _recursoService = recursoService;
        _politicaAccesoService = politicaAccesoService;
        _credencialTemporalService = credencialTemporalService;
        _auditService = auditService;
    }

    public async Task<List<SolicitudAcceso>> GetByUsuarioAsync(string usuarioId)
    {
        if (!ObjectId.TryParse(usuarioId, out _))
        {
            return new List<SolicitudAcceso>();
        }

        return await _solicitudes
            .Find(s => s.UsuarioId == usuarioId)
            .SortByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<SolicitudAcceso>> GetPendientesAsync()
    {
        return await _solicitudes
            .Find(s => s.Estado == EstadoSolicitud.Pendiente)
            .SortBy(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<SolicitudAcceso>> GetPendientesParaRevisionAsync(
        string revisorId,
        bool esAdministrador)
    {
        if (!ObjectId.TryParse(revisorId, out _))
        {
            return new List<SolicitudAcceso>();
        }

        if (esAdministrador)
        {
            return await GetPendientesAsync();
        }

        var recursosResponsable = await _recursos
            .Find(r => r.ResponsableId == revisorId && r.Activo)
            .Project(r => r.Id)
            .ToListAsync();

        var recursoIds = recursosResponsable
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .ToList();

        if (!recursoIds.Any())
        {
            return new List<SolicitudAcceso>();
        }

        return await _solicitudes
            .Find(s =>
                s.Estado == EstadoSolicitud.Pendiente &&
                recursoIds.Contains(s.RecursoId)
            )
            .SortBy(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<SolicitudAcceso?> GetByIdAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return null;
        }

        return await _solicitudes
            .Find(s => s.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<(bool TienePermiso, string Message)> PuedeRevisarSolicitudAsync(
        string solicitudId,
        string revisorId,
        bool esAdministrador)
    {
        if (!ObjectId.TryParse(solicitudId, out _) ||
            !ObjectId.TryParse(revisorId, out var revisorObjectId))
        {
            return (false, "La solicitud o el revisor no tienen un identificador válido.");
        }

        var solicitud = await GetByIdAsync(solicitudId);

        if (solicitud is null)
        {
            return (false, "La solicitud no existe.");
        }

        if (esAdministrador)
        {
            return (true, "El administrador tiene permiso global para revisar la solicitud.");
        }

        var recurso = await _recursos
            .Find(r => r.Id == solicitud.RecursoId)
            .FirstOrDefaultAsync();

        if (recurso is null)
        {
            return (false, "El recurso asociado a la solicitud no existe.");
        }

        if (string.IsNullOrWhiteSpace(recurso.ResponsableId) ||
            !ObjectId.TryParse(recurso.ResponsableId, out var responsableObjectId))
        {
            return (false, "El recurso no tiene un responsable válido asignado.");
        }

        if (responsableObjectId != revisorObjectId)
        {
            return (false, "No tienes permiso para revisar solicitudes de este recurso.");
        }

        return (true, "El aprobador tiene permiso para revisar la solicitud.");
    }

    public async Task<SolicitudAcceso?> GetByIdAndUsuarioAsync(string id, string usuarioId)
    {
        if (!ObjectId.TryParse(id, out _) || !ObjectId.TryParse(usuarioId, out _))
        {
            return null;
        }

        return await _solicitudes
            .Find(s => s.Id == id && s.UsuarioId == usuarioId)
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string Message)> CreateAsync(SolicitudAcceso solicitud)
    {
        if (!ObjectId.TryParse(solicitud.UsuarioId, out _))
        {
            return (false, "El usuario de la solicitud no es válido.");
        }

        if (!ObjectId.TryParse(solicitud.RecursoId, out _))
        {
            return (false, "El recurso solicitado no es válido.");
        }

        if (string.IsNullOrWhiteSpace(solicitud.Motivo))
        {
            return (false, "El motivo de la solicitud es obligatorio.");
        }

        if (solicitud.DuracionSolicitadaMin <= 0)
        {
            return (false, "La duración solicitada debe ser mayor que cero.");
        }

        var recurso = await _recursoService.GetByIdAsync(solicitud.RecursoId);

        if (recurso is null)
        {
            return (false, "El recurso solicitado no existe.");
        }

        if (!recurso.Activo)
        {
            return (false, "El recurso solicitado no se encuentra activo.");
        }

        solicitud.Estado = EstadoSolicitud.Pendiente;
        solicitud.AprobadorId = null;
        solicitud.Observacion = null;
        solicitud.DuracionAprobadaMin = null;
        solicitud.CreatedAt = DateTime.UtcNow;
        solicitud.ResolvedAt = null;

        if (string.IsNullOrWhiteSpace(solicitud.Prioridad))
        {
            solicitud.Prioridad = "Normal";
        }

        await _solicitudes.InsertOneAsync(solicitud);

        await _auditService.RegistrarEventoAsync(
            accion: "SOLICITUD_CREADA",
            entidadTipo: "SolicitudAcceso",
            resultado: ResultadoAuditoria.Exitoso,
            actorUserId: solicitud.UsuarioId,
            entidadId: solicitud.Id,
            detalle: new Dictionary<string, string>
            {
                { "recurso_id", solicitud.RecursoId },
                { "motivo", solicitud.Motivo },
                { "duracion_solicitada_min", solicitud.DuracionSolicitadaMin.ToString() },
                { "prioridad", solicitud.Prioridad },
                { "estado", solicitud.Estado.ToString() },
                { "recurso_nombre", recurso.Nombre },
                { "sensibilidad", recurso.Sensibilidad.ToString() }
            }
        );

        return (true, "Solicitud creada correctamente.");
    }
    
    public async Task<(bool Success, string Message, string? TokenPlano, string? CredencialId)> AprobarAsync(
        string solicitudId,
        string aprobadorId,
        bool esAdministrador,
        int duracionAprobadaMin,
        string? observacion)
    {
        if (!ObjectId.TryParse(solicitudId, out _) || !ObjectId.TryParse(aprobadorId, out _))
        {
            return (false, "La solicitud o el aprobador no tienen un identificador válido.", null, null);
        }

        if (duracionAprobadaMin <= 0)
        {
            return (false, "La duración aprobada debe ser mayor que cero.", null, null);
        }

        var solicitud = await GetByIdAsync(solicitudId);

        if (solicitud is null)
        {
            return (false, "La solicitud no existe.", null, null);
        }

        if (solicitud.Estado != EstadoSolicitud.Pendiente)
        {
            return (false, "Solo se pueden aprobar solicitudes pendientes.", null, null);
        }

        var recurso = await _recursoService.GetByIdAsync(solicitud.RecursoId);

        if (recurso is null)
        {
            return (false, "El recurso asociado a la solicitud no existe.", null, null);
        }

        if (!recurso.Activo)
        {
            return (false, "No se puede aprobar una solicitud para un recurso inactivo.", null, null);
        }

        var permiso = await PuedeRevisarSolicitudAsync(
            solicitudId,
            aprobadorId,
            esAdministrador
        );

        if (!permiso.TienePermiso)
        {
            await _auditService.RegistrarEventoAsync(
                accion: "SOLICITUD_APROBACION_NO_AUTORIZADA",
                entidadTipo: "SolicitudAcceso",
                resultado: ResultadoAuditoria.Fallido,
                actorUserId: aprobadorId,
                entidadId: solicitudId,
                detalle: new Dictionary<string, string>
                {
                    { "usuario_id", solicitud.UsuarioId },
                    { "recurso_id", solicitud.RecursoId },
                    { "recurso_nombre", recurso.Nombre },
                    { "motivo_bloqueo", permiso.Message }
                }
            );

            return (false, permiso.Message, null, null);
        }

        var politica = await _politicaAccesoService.GetBySensibilidadAsync(recurso.Sensibilidad);

        if (politica is null)
        {
            return (false, "No existe una política asociada a la sensibilidad del recurso.", null, null);
        }

        var duracionFinalMin = Math.Min(duracionAprobadaMin, politica.DuracionMaxMin);
        var resolvedAt = DateTime.UtcNow;

        using var session = await _mongoClient.StartSessionAsync();

        try
        {
            session.StartTransaction();

            var update = Builders<SolicitudAcceso>.Update
                .Set(s => s.Estado, EstadoSolicitud.Aprobada)
                .Set(s => s.AprobadorId, aprobadorId)
                .Set(s => s.Observacion, observacion)
                .Set(s => s.DuracionAprobadaMin, duracionFinalMin)
                .Set(s => s.ResolvedAt, resolvedAt);

            var result = await _solicitudes.UpdateOneAsync(
                session,
                s => s.Id == solicitudId && s.Estado == EstadoSolicitud.Pendiente,
                update
            );

            if (result.ModifiedCount == 0)
            {
                await session.AbortTransactionAsync();
                return (false, "No se pudo aprobar la solicitud.", null, null);
            }

            var emision = await _credencialTemporalService.EmitirAsync(
                solicitud,
                politica,
                duracionFinalMin,
                session
            );

            if (!emision.Success || emision.Credencial is null || string.IsNullOrWhiteSpace(emision.TokenPlano))
            {
                await session.AbortTransactionAsync();
                return (false, emision.Message, null, null);
            }

            await _auditService.RegistrarEventoAsync(
                accion: "SOLICITUD_APROBADA",
                entidadTipo: "SolicitudAcceso",
                resultado: ResultadoAuditoria.Exitoso,
                actorUserId: aprobadorId,
                entidadId: solicitudId,
                detalle: new Dictionary<string, string>
                {
                    { "usuario_id", solicitud.UsuarioId },
                    { "recurso_id", solicitud.RecursoId },
                    { "recurso_nombre", recurso.Nombre },
                    { "sensibilidad", recurso.Sensibilidad.ToString() },
                    { "duracion_solicitada_min", solicitud.DuracionSolicitadaMin.ToString() },
                    { "duracion_aprobada_min", duracionFinalMin.ToString() }
                },
                session: session
            );

            await _auditService.RegistrarEventoAsync(
                accion: "CREDENCIAL_EMITIDA",
                entidadTipo: "CredencialTemporal",
                resultado: ResultadoAuditoria.Exitoso,
                actorUserId: aprobadorId,
                entidadId: emision.Credencial.Id,
                detalle: new Dictionary<string, string>
                {
                    { "solicitud_id", solicitudId },
                    { "usuario_id", solicitud.UsuarioId },
                    { "recurso_id", solicitud.RecursoId },
                    { "expires_at", emision.Credencial.ExpiresAt.ToString("O") },
                    { "max_usos", emision.Credencial.MaxUsos.ToString() }
                },
                session: session
            );

            await session.CommitTransactionAsync();

            return (
                true,
                "Solicitud aprobada y credencial temporal emitida correctamente.",
                emision.TokenPlano,
                emision.Credencial.Id
            );
        }
        catch
        {
            await session.AbortTransactionAsync();
            throw;
        }
    }

    public async Task<(bool Success, string Message)> RechazarAsync(
        string solicitudId,
        string aprobadorId,
        string observacion)
    {
        if (!ObjectId.TryParse(solicitudId, out _) || !ObjectId.TryParse(aprobadorId, out _))
        {
            return (false, "La solicitud o el aprobador no tienen un identificador válido.");
        }

        if (string.IsNullOrWhiteSpace(observacion))
        {
            return (false, "La observación es obligatoria para rechazar una solicitud.");
        }

        var solicitud = await GetByIdAsync(solicitudId);

        if (solicitud is null)
        {
            return (false, "La solicitud no existe.");
        }

        if (solicitud.Estado != EstadoSolicitud.Pendiente)
        {
            return (false, "Solo se pueden rechazar solicitudes pendientes.");
        }

        var resolvedAt = DateTime.UtcNow;

        var update = Builders<SolicitudAcceso>.Update
            .Set(s => s.Estado, EstadoSolicitud.Rechazada)
            .Set(s => s.AprobadorId, aprobadorId)
            .Set(s => s.Observacion, observacion)
            .Set(s => s.ResolvedAt, resolvedAt);

        var result = await _solicitudes.UpdateOneAsync(
            s => s.Id == solicitudId && s.Estado == EstadoSolicitud.Pendiente,
            update
        );

        if (result.ModifiedCount == 0)
        {
            return (false, "No se pudo rechazar la solicitud.");
        }

        await _auditService.RegistrarEventoAsync(
            accion: "SOLICITUD_RECHAZADA",
            entidadTipo: "SolicitudAcceso",
            resultado: ResultadoAuditoria.Exitoso,
            actorUserId: aprobadorId,
            entidadId: solicitudId,
            detalle: new Dictionary<string, string>
            {
                { "usuario_id", solicitud.UsuarioId },
                { "recurso_id", solicitud.RecursoId },
                { "motivo_rechazo", observacion }
            }
        );

        return (true, "Solicitud rechazada correctamente.");
    }
}