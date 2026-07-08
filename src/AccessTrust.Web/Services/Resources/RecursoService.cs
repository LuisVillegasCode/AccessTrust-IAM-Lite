using AccessTrust.Web.Data;
using AccessTrust.Web.Models;
using AccessTrust.Web.Services.Audit;
using AccessTrust.Web.Services.Policies;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AccessTrust.Web.Services.Resources;

public class RecursoService : IRecursoService
{
    private readonly IMongoCollection<Recurso> _recursos;
    private readonly IMongoCollection<Usuario> _usuarios;
    private readonly IPoliticaAccesoService _politicaAccesoService;
    private readonly IAuditService _auditService;

    public RecursoService(
        IMongoDatabase database,
        IPoliticaAccesoService politicaAccesoService,
        IAuditService auditService)
    {
        _recursos = database.GetCollection<Recurso>(MongoCollections.Recursos);
        _usuarios = database.GetCollection<Usuario>(MongoCollections.Usuarios);
        _politicaAccesoService = politicaAccesoService;
        _auditService = auditService;
    }

    public async Task<List<Recurso>> GetAllAsync()
    {
        return await _recursos
            .Find(Builders<Recurso>.Filter.Empty)
            .SortByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Recurso>> GetActivosAsync()
    {
        return await _recursos
            .Find(r => r.Activo)
            .SortBy(r => r.Nombre)
            .ToListAsync();
    }

    public async Task<Recurso?> GetByIdAsync(string id)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return null;
        }

        return await _recursos
            .Find(r => r.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Usuario>> GetAprobadoresActivosAsync()
    {
        var filtro =
            Builders<Usuario>.Filter.Eq(u => u.Estado, EstadoCuenta.Activo) &
            Builders<Usuario>.Filter.AnyEq(u => u.Roles, "Aprobador");

        return await _usuarios
            .Find(filtro)
            .SortBy(u => u.Nombre)
            .ToListAsync();
    }

    public async Task CreateAsync(Recurso recurso)
    {
        var politica = await _politicaAccesoService
            .GetBySensibilidadAsync(recurso.Sensibilidad);

        recurso.PoliticaId = politica?.Id;
        recurso.CreatedAt = DateTime.UtcNow;

        await _recursos.InsertOneAsync(recurso);
    }

    public async Task<(bool Success, string Message)> UpdateAsync(
        string id,
        Recurso recurso,
        string actorUserId)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return (false, "El identificador del recurso no es válido.");
        }

        var recursoActual = await GetByIdAsync(id);

        if (recursoActual is null)
        {
            return (false, "El recurso no existe.");
        }

        if (string.IsNullOrWhiteSpace(recurso.ResponsableId) ||
            !ObjectId.TryParse(recurso.ResponsableId, out _))
        {
            return (false, "Debe seleccionar un responsable válido para el recurso.");
        }

        var filtroResponsable =
            Builders<Usuario>.Filter.Eq(u => u.Id, recurso.ResponsableId) &
            Builders<Usuario>.Filter.Eq(u => u.Estado, EstadoCuenta.Activo) &
            Builders<Usuario>.Filter.AnyEq(u => u.Roles, "Aprobador");

        var responsableNuevo = await _usuarios
            .Find(filtroResponsable)
            .FirstOrDefaultAsync();

        if (responsableNuevo is null)
        {
            return (false, "El responsable seleccionado debe ser un usuario activo con rol Aprobador.");
        }

        var politica = await _politicaAccesoService
            .GetBySensibilidadAsync(recurso.Sensibilidad);

        var responsableAnteriorId = recursoActual.ResponsableId ?? string.Empty;
        var responsableNuevoId = recurso.ResponsableId;

        var update = Builders<Recurso>.Update
            .Set(r => r.Nombre, recurso.Nombre)
            .Set(r => r.Tipo, recurso.Tipo)
            .Set(r => r.Sensibilidad, recurso.Sensibilidad)
            .Set(r => r.Activo, recurso.Activo)
            .Set(r => r.ResponsableId, responsableNuevoId)
            .Set(r => r.PoliticaId, politica?.Id)
            .Set(r => r.UpdatedAt, DateTime.UtcNow);

        var result = await _recursos.UpdateOneAsync(
            r => r.Id == id,
            update
        );

        if (result.MatchedCount == 0)
        {
            return (false, "El recurso no existe.");
        }

        if (responsableAnteriorId != responsableNuevoId)
        {
            await _auditService.RegistrarEventoAsync(
                accion: "RECURSO_RESPONSABLE_ACTUALIZADO",
                entidadTipo: "Recurso",
                resultado: ResultadoAuditoria.Exitoso,
                actorUserId: actorUserId,
                entidadId: id,
                detalle: new Dictionary<string, string>
                {
                    { "recurso_nombre", recurso.Nombre },
                    { "responsable_anterior_id", responsableAnteriorId },
                    { "responsable_nuevo_id", responsableNuevoId },
                    { "responsable_nuevo_nombre", responsableNuevo.Nombre },
                    { "responsable_nuevo_correo", responsableNuevo.Correo }
                }
            );
        }

        return (true, "Recurso actualizado correctamente.");
    }
}