using AccessTrust.Web.Data;
using AccessTrust.Web.Models;
using AccessTrust.Web.Services.Audit;
using AccessTrust.Web.Services.Resources;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AccessTrust.Web.Services.Requests;

public class SolicitudAccesoService : ISolicitudAccesoService
{
    private readonly IMongoCollection<SolicitudAcceso> _solicitudes;
    private readonly IRecursoService _recursoService;
    private readonly IAuditService _auditService;

    public SolicitudAccesoService(
        IMongoDatabase database,
        IRecursoService recursoService,
        IAuditService auditService)
    {
        _solicitudes = database.GetCollection<SolicitudAcceso>(MongoCollections.SolicitudesAcceso);
        _recursoService = recursoService;
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
}