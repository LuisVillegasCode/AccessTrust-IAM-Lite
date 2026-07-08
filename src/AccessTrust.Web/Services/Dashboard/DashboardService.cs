using AccessTrust.Web.Data;
using AccessTrust.Web.ViewModels.Dashboard;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AccessTrust.Web.Services.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly IMongoCollection<BsonDocument> _usuarios;
    private readonly IMongoCollection<BsonDocument> _recursos;
    private readonly IMongoCollection<BsonDocument> _solicitudes;
    private readonly IMongoCollection<BsonDocument> _credenciales;
    private readonly IMongoCollection<BsonDocument> _ticketsExternos;
    private readonly IMongoCollection<BsonDocument> _eventosAuditoria;
    private readonly IMongoCollection<BsonDocument> _alertasSeguridad;

    public DashboardService(IMongoDatabase database)
    {
        _usuarios = database.GetCollection<BsonDocument>(MongoCollections.Usuarios);
        _recursos = database.GetCollection<BsonDocument>(MongoCollections.Recursos);
        _solicitudes = database.GetCollection<BsonDocument>(MongoCollections.SolicitudesAcceso);
        _credenciales = database.GetCollection<BsonDocument>(MongoCollections.CredencialesTemporales);
        _ticketsExternos = database.GetCollection<BsonDocument>(MongoCollections.TicketsAccesoExterno);
        _eventosAuditoria = database.GetCollection<BsonDocument>(MongoCollections.EventosAuditoria);
        _alertasSeguridad = database.GetCollection<BsonDocument>(MongoCollections.AlertasSeguridad);
    }

    public async Task<SolicitanteDashboardViewModel> GetSolicitanteDashboardAsync(
        string usuarioId,
        string usuarioNombre,
        string usuarioCorreo)
    {
        var usuarioObjectId = ToObjectId(usuarioId);
        var now = DateTime.UtcNow;
        var proximoVencimiento = now.AddMinutes(30);

        var filtroUsuario = Builders<BsonDocument>.Filter.Eq("UsuarioId", usuarioObjectId);

        var solicitudesPendientes = await _solicitudes.CountDocumentsAsync(
            filtroUsuario & Builders<BsonDocument>.Filter.Eq("Estado", "Pendiente")
        );

        var solicitudesAprobadas = await _solicitudes.CountDocumentsAsync(
            filtroUsuario & Builders<BsonDocument>.Filter.Eq("Estado", "Aprobada")
        );

        var solicitudesRechazadas = await _solicitudes.CountDocumentsAsync(
            filtroUsuario & Builders<BsonDocument>.Filter.Eq("Estado", "Rechazada")
        );

        var filtroCredencialesActivas =
            filtroUsuario &
            Builders<BsonDocument>.Filter.Eq("Estado", "Activa") &
            Builders<BsonDocument>.Filter.Gt("ExpiresAt", now);

        var accesosActivos = await _credenciales.CountDocumentsAsync(filtroCredencialesActivas);

        var accesosProximosAExpirar = await _credenciales.CountDocumentsAsync(
            filtroCredencialesActivas &
            Builders<BsonDocument>.Filter.Lte("ExpiresAt", proximoVencimiento)
        );

        var solicitudesRecientesDocs = await _solicitudes
            .Find(filtroUsuario)
            .Sort(Builders<BsonDocument>.Sort.Descending("CreatedAt"))
            .Limit(5)
            .ToListAsync();

        var accesosActivosDocs = await _credenciales
            .Find(filtroCredencialesActivas)
            .Sort(Builders<BsonDocument>.Sort.Ascending("ExpiresAt"))
            .Limit(5)
            .ToListAsync();

        return new SolicitanteDashboardViewModel
        {
            UsuarioNombre = usuarioNombre,
            UsuarioCorreo = usuarioCorreo,
            GeneradoAt = now,
            SolicitudesPendientes = (int)solicitudesPendientes,
            SolicitudesAprobadas = (int)solicitudesAprobadas,
            SolicitudesRechazadas = (int)solicitudesRechazadas,
            AccesosActivos = (int)accesosActivos,
            AccesosProximosAExpirar = (int)accesosProximosAExpirar,
            SolicitudesRecientes = await MapSolicitudesRecientesAsync(solicitudesRecientesDocs),
            AccesosActivosRecientes = await MapAccesosActivosAsync(accesosActivosDocs, proximoVencimiento)
        };
    }

    public async Task<AprobadorDashboardViewModel> GetAprobadorDashboardAsync(
        string aprobadorId,
        string usuarioNombre,
        string usuarioCorreo)
    {
        var aprobadorObjectId = ToObjectId(aprobadorId);
        var now = DateTime.UtcNow;
        var desde = now.AddDays(-7);

        var recursosDocs = await _recursos
            .Find(Builders<BsonDocument>.Filter.Eq("ResponsableId", aprobadorObjectId))
            .Sort(Builders<BsonDocument>.Sort.Ascending("Nombre"))
            .Limit(5)
            .ToListAsync();

        var recursoIds = recursosDocs
            .Select(r => r.GetValue("_id").AsObjectId)
            .ToList();

        var filtroRecursosAsignados = recursoIds.Count == 0
            ? Builders<BsonDocument>.Filter.Where(_ => false)
            : Builders<BsonDocument>.Filter.In("RecursoId", recursoIds);

        var filtroPendientes =
            filtroRecursosAsignados &
            Builders<BsonDocument>.Filter.Eq("Estado", "Pendiente");

        var solicitudesPendientesAsignadas = await _solicitudes.CountDocumentsAsync(filtroPendientes);

        var solicitudesPendientesDocs = await _solicitudes
            .Find(filtroPendientes)
            .Sort(Builders<BsonDocument>.Sort.Ascending("CreatedAt"))
            .Limit(5)
            .ToListAsync();

        var solicitudesAprobadasRecientes = await _solicitudes.CountDocumentsAsync(
            Builders<BsonDocument>.Filter.Eq("AprobadorId", aprobadorObjectId) &
            Builders<BsonDocument>.Filter.Eq("Estado", "Aprobada") &
            Builders<BsonDocument>.Filter.Gte("ResolvedAt", desde)
        );

        var solicitudesRechazadasRecientes = await _solicitudes.CountDocumentsAsync(
            Builders<BsonDocument>.Filter.Eq("AprobadorId", aprobadorObjectId) &
            Builders<BsonDocument>.Filter.Eq("Estado", "Rechazada") &
            Builders<BsonDocument>.Filter.Gte("ResolvedAt", desde)
        );

        var credencialesEmitidasRecientes = await _eventosAuditoria.CountDocumentsAsync(
            Builders<BsonDocument>.Filter.Eq("ActorUserId", aprobadorObjectId) &
            Builders<BsonDocument>.Filter.Eq("Accion", "SOLICITUD_APROBADA") &
            Builders<BsonDocument>.Filter.Gte("CreatedAt", desde)
        );

        var actividadDocs = await _eventosAuditoria
            .Find(
                Builders<BsonDocument>.Filter.Eq("ActorUserId", aprobadorObjectId) &
                Builders<BsonDocument>.Filter.Gte("CreatedAt", desde)
            )
            .Sort(Builders<BsonDocument>.Sort.Descending("CreatedAt"))
            .Limit(8)
            .ToListAsync();

        return new AprobadorDashboardViewModel
        {
            UsuarioNombre = usuarioNombre,
            UsuarioCorreo = usuarioCorreo,
            GeneradoAt = now,
            SolicitudesPendientesAsignadas = (int)solicitudesPendientesAsignadas,
            RecursosBajoResponsabilidad = recursoIds.Count,
            SolicitudesAprobadasRecientes = (int)solicitudesAprobadasRecientes,
            SolicitudesRechazadasRecientes = (int)solicitudesRechazadasRecientes,
            CredencialesEmitidasRecientes = (int)credencialesEmitidasRecientes,
            SolicitudesPendientes = await MapSolicitudesPendientesAsync(solicitudesPendientesDocs),
            RecursosResponsable = recursosDocs.Select(MapRecursoResponsable).ToList(),
            ActividadReciente = actividadDocs.Select(MapEventoAuditoria).ToList()
        };
    }

    public async Task<AdministradorDashboardViewModel> GetAdministradorDashboardAsync(
        string usuarioNombre,
        string usuarioCorreo)
    {
        var now = DateTime.UtcNow;
        var inicioHoy = now.Date;
        var desde = now.AddDays(-7);

        var usuariosActivos = await _usuarios.CountDocumentsAsync(
            Builders<BsonDocument>.Filter.Eq("Estado", "Activo")
        );

        var recursosActivos = await _recursos.CountDocumentsAsync(
            Builders<BsonDocument>.Filter.Eq("Activo", true)
        );

        var solicitudesPendientes = await _solicitudes.CountDocumentsAsync(
            Builders<BsonDocument>.Filter.Eq("Estado", "Pendiente")
        );

        var credencialesActivas = await _credenciales.CountDocumentsAsync(
            Builders<BsonDocument>.Filter.Eq("Estado", "Activa") &
            Builders<BsonDocument>.Filter.Gt("ExpiresAt", now)
        );

        var ticketsExternosEmitidos = await _ticketsExternos.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);

        var eventosAuditoriaHoy = await _eventosAuditoria.CountDocumentsAsync(
            Builders<BsonDocument>.Filter.Gte("CreatedAt", inicioHoy)
        );

        var eventosFallidosRecientes = await _eventosAuditoria.CountDocumentsAsync(
            Builders<BsonDocument>.Filter.Eq("Resultado", "Fallido") &
            Builders<BsonDocument>.Filter.Gte("CreatedAt", desde)
        );

        var intentosNoAutorizadosRecientes = await _eventosAuditoria.CountDocumentsAsync(
            Builders<BsonDocument>.Filter.In(
                "Accion",
                new[]
                {
                    "SOLICITUD_REVISION_NO_AUTORIZADA",
                    "SOLICITUD_APROBACION_NO_AUTORIZADA",
                    "SOLICITUD_RECHAZO_NO_AUTORIZADO"
                }
            ) &
            Builders<BsonDocument>.Filter.Gte("CreatedAt", desde)
        );

        var eventosRecientesDocs = await _eventosAuditoria
            .Find(FilterDefinition<BsonDocument>.Empty)
            .Sort(Builders<BsonDocument>.Sort.Descending("CreatedAt"))
            .Limit(8)
            .ToListAsync();

        var alertasDocs = await _alertasSeguridad
            .Find(FilterDefinition<BsonDocument>.Empty)
            .Sort(Builders<BsonDocument>.Sort.Descending("CreatedAt"))
            .Limit(5)
            .ToListAsync();

        var recursosCriticosDocs = await _recursos
            .Find(
                Builders<BsonDocument>.Filter.In("Sensibilidad", new[] { "Alta", "Critica", "Crítica" }) &
                Builders<BsonDocument>.Filter.Eq("Activo", true)
            )
            .Sort(Builders<BsonDocument>.Sort.Descending("Sensibilidad"))
            .Limit(5)
            .ToListAsync();

        return new AdministradorDashboardViewModel
        {
            UsuarioNombre = usuarioNombre,
            UsuarioCorreo = usuarioCorreo,
            GeneradoAt = now,
            UsuariosActivos = (int)usuariosActivos,
            RecursosActivos = (int)recursosActivos,
            SolicitudesPendientesTotales = (int)solicitudesPendientes,
            CredencialesActivas = (int)credencialesActivas,
            TicketsExternosEmitidos = (int)ticketsExternosEmitidos,
            EventosAuditoriaHoy = (int)eventosAuditoriaHoy,
            EventosFallidosRecientes = (int)eventosFallidosRecientes,
            IntentosNoAutorizadosRecientes = (int)intentosNoAutorizadosRecientes,
            IntegridadAuditoriaValida = true,
            EventosRecientes = eventosRecientesDocs.Select(MapEventoAuditoria).ToList(),
            AlertasRecientes = alertasDocs.Select(MapAlerta).ToList(),
            RecursosCriticos = await MapRecursosCriticosAsync(recursosCriticosDocs)
        };
    }

    private async Task<List<SolicitudRecienteDashboardItemViewModel>> MapSolicitudesRecientesAsync(
        List<BsonDocument> documentos)
    {
        var resultado = new List<SolicitudRecienteDashboardItemViewModel>();

        foreach (var doc in documentos)
        {
            var recurso = await GetRecursoAsync(GetObjectIdAsString(doc, "RecursoId"));

            resultado.Add(new SolicitudRecienteDashboardItemViewModel
            {
                Id = GetObjectIdAsString(doc, "_id"),
                RecursoNombre = GetString(recurso, "Nombre", "Recurso no disponible"),
                RecursoTipo = GetString(recurso, "Tipo", "-"),
                Sensibilidad = GetString(recurso, "Sensibilidad", "-"),
                Estado = GetString(doc, "Estado", "-"),
                Prioridad = GetString(doc, "Prioridad", "-"),
                DuracionSolicitadaMin = GetInt(doc, "DuracionSolicitadaMin"),
                CreatedAt = GetDate(doc, "CreatedAt"),
                ResolvedAt = GetNullableDate(doc, "ResolvedAt")
            });
        }

        return resultado;
    }

    private async Task<List<AccesoActivoDashboardItemViewModel>> MapAccesosActivosAsync(
        List<BsonDocument> documentos,
        DateTime proximoVencimiento)
    {
        var resultado = new List<AccesoActivoDashboardItemViewModel>();

        foreach (var doc in documentos)
        {
            var recurso = await GetRecursoAsync(GetObjectIdAsString(doc, "RecursoId"));
            var expiresAt = GetDate(doc, "ExpiresAt");

            resultado.Add(new AccesoActivoDashboardItemViewModel
            {
                CredencialId = GetObjectIdAsString(doc, "_id"),
                RecursoNombre = GetString(recurso, "Nombre", "Recurso no disponible"),
                RecursoTipo = GetString(recurso, "Tipo", "-"),
                Sensibilidad = GetString(recurso, "Sensibilidad", "-"),
                Estado = GetString(doc, "Estado", "-"),
                IssuedAt = GetDate(doc, "IssuedAt"),
                ExpiresAt = expiresAt,
                UsosRealizados = GetInt(doc, "UsosRealizados", "UsesCount", "UsoActual"),
                UsosMaximos = GetInt(doc, "UsosMaximos", "MaxUsos", "MaxUses"),
                ProximoAExpirar = expiresAt <= proximoVencimiento
            });
        }

        return resultado;
    }

    private async Task<List<SolicitudPendienteDashboardItemViewModel>> MapSolicitudesPendientesAsync(
        List<BsonDocument> documentos)
    {
        var resultado = new List<SolicitudPendienteDashboardItemViewModel>();

        foreach (var doc in documentos)
        {
            var usuario = await GetUsuarioAsync(GetObjectIdAsString(doc, "UsuarioId"));
            var recurso = await GetRecursoAsync(GetObjectIdAsString(doc, "RecursoId"));

            resultado.Add(new SolicitudPendienteDashboardItemViewModel
            {
                Id = GetObjectIdAsString(doc, "_id"),
                UsuarioId = GetObjectIdAsString(doc, "UsuarioId"),
                UsuarioNombre = GetString(usuario, "Nombre", "Usuario no disponible"),
                UsuarioCorreo = GetString(usuario, "Correo", "-"),
                RecursoNombre = GetString(recurso, "Nombre", "Recurso no disponible"),
                RecursoTipo = GetString(recurso, "Tipo", "-"),
                Sensibilidad = GetString(recurso, "Sensibilidad", "-"),
                Prioridad = GetString(doc, "Prioridad", "-"),
                DuracionSolicitadaMin = GetInt(doc, "DuracionSolicitadaMin"),
                CreatedAt = GetDate(doc, "CreatedAt")
            });
        }

        return resultado;
    }

    private RecursoResponsableDashboardItemViewModel MapRecursoResponsable(BsonDocument doc)
    {
        return new RecursoResponsableDashboardItemViewModel
        {
            Id = GetObjectIdAsString(doc, "_id"),
            Nombre = GetString(doc, "Nombre", "-"),
            Tipo = GetString(doc, "Tipo", "-"),
            Sensibilidad = GetString(doc, "Sensibilidad", "-"),
            Activo = GetBool(doc, "Activo"),
            CreatedAt = GetDate(doc, "CreatedAt")
        };
    }

    private EventoAuditoriaDashboardItemViewModel MapEventoAuditoria(BsonDocument doc)
    {
        return new EventoAuditoriaDashboardItemViewModel
        {
            Id = GetObjectIdAsString(doc, "_id"),
            Seq = GetLong(doc, "Seq"),
            ActorUserId = GetObjectIdAsString(doc, "ActorUserId"),
            Accion = GetString(doc, "Accion", "-"),
            EntidadTipo = GetString(doc, "EntidadTipo", "-"),
            EntidadId = GetObjectIdAsString(doc, "EntidadId"),
            Resultado = GetString(doc, "Resultado", "-"),
            CreatedAt = GetDate(doc, "CreatedAt")
        };
    }

    private AlertaSeguridadDashboardItemViewModel MapAlerta(BsonDocument doc)
    {
        return new AlertaSeguridadDashboardItemViewModel
        {
            Id = GetObjectIdAsString(doc, "_id"),
            Severidad = GetString(doc, "Severidad", "-"),
            Mensaje = GetString(doc, "Mensaje", "-"),
            Estado = GetString(doc, "Estado", "-"),
            CreatedAt = GetDate(doc, "CreatedAt")
        };
    }

    private async Task<List<RecursoCriticoDashboardItemViewModel>> MapRecursosCriticosAsync(
        List<BsonDocument> documentos)
    {
        var resultado = new List<RecursoCriticoDashboardItemViewModel>();

        foreach (var doc in documentos)
        {
            var responsableId = GetObjectIdAsString(doc, "ResponsableId");
            var responsable = await GetUsuarioAsync(responsableId);

            resultado.Add(new RecursoCriticoDashboardItemViewModel
            {
                Id = GetObjectIdAsString(doc, "_id"),
                Nombre = GetString(doc, "Nombre", "-"),
                Tipo = GetString(doc, "Tipo", "-"),
                Sensibilidad = GetString(doc, "Sensibilidad", "-"),
                ResponsableId = responsableId,
                ResponsableNombre = GetString(responsable, "Nombre", "Sin responsable"),
                ResponsableCorreo = GetString(responsable, "Correo", "-"),
                Activo = GetBool(doc, "Activo")
            });
        }

        return resultado;
    }

    private async Task<BsonDocument?> GetRecursoAsync(string recursoId)
    {
        if (!ObjectId.TryParse(recursoId, out var objectId))
        {
            return null;
        }

        return await _recursos
            .Find(Builders<BsonDocument>.Filter.Eq("_id", objectId))
            .FirstOrDefaultAsync();
    }

    private async Task<BsonDocument?> GetUsuarioAsync(string usuarioId)
    {
        if (!ObjectId.TryParse(usuarioId, out var objectId))
        {
            return null;
        }

        return await _usuarios
            .Find(Builders<BsonDocument>.Filter.Eq("_id", objectId))
            .FirstOrDefaultAsync();
    }

    private static ObjectId ToObjectId(string id)
    {
        return ObjectId.TryParse(id, out var objectId)
            ? objectId
            : ObjectId.Empty;
    }

    private static string GetObjectIdAsString(BsonDocument? doc, string fieldName)
    {
        if (doc is null || !doc.Contains(fieldName) || doc[fieldName].IsBsonNull)
        {
            return string.Empty;
        }

        var value = doc[fieldName];

        if (value.IsObjectId)
        {
            return value.AsObjectId.ToString();
        }

        return value.ToString() ?? string.Empty;
    }

    private static string GetString(BsonDocument? doc, string fieldName, string defaultValue)
    {
        if (doc is null || !doc.Contains(fieldName) || doc[fieldName].IsBsonNull)
        {
            return defaultValue;
        }

        return doc[fieldName].ToString() ?? defaultValue;
    }

    private static int GetInt(BsonDocument doc, params string[] fieldNames)
    {
        foreach (var fieldName in fieldNames)
        {
            if (!doc.Contains(fieldName) || doc[fieldName].IsBsonNull)
            {
                continue;
            }

            var value = doc[fieldName];

            if (value.IsInt32)
            {
                return value.AsInt32;
            }

            if (value.IsInt64)
            {
                return (int)value.AsInt64;
            }

            if (int.TryParse(value.ToString(), out var result))
            {
                return result;
            }
        }

        return 0;
    }

    private static long GetLong(BsonDocument doc, string fieldName)
    {
        if (!doc.Contains(fieldName) || doc[fieldName].IsBsonNull)
        {
            return 0;
        }

        var value = doc[fieldName];

        if (value.IsInt64)
        {
            return value.AsInt64;
        }

        if (value.IsInt32)
        {
            return value.AsInt32;
        }

        return long.TryParse(value.ToString(), out var result) ? result : 0;
    }

    private static bool GetBool(BsonDocument doc, string fieldName)
    {
        if (!doc.Contains(fieldName) || doc[fieldName].IsBsonNull)
        {
            return false;
        }

        var value = doc[fieldName];

        if (value.IsBoolean)
        {
            return value.AsBoolean;
        }

        return bool.TryParse(value.ToString(), out var result) && result;
    }

    private static DateTime GetDate(BsonDocument doc, string fieldName)
    {
        if (!doc.Contains(fieldName) || doc[fieldName].IsBsonNull)
        {
            return DateTime.MinValue;
        }

        var value = doc[fieldName];

        if (value.IsValidDateTime)
        {
            return value.ToUniversalTime();
        }

        return DateTime.TryParse(value.ToString(), out var result)
            ? result.ToUniversalTime()
            : DateTime.MinValue;
    }

    private static DateTime? GetNullableDate(BsonDocument doc, string fieldName)
    {
        if (!doc.Contains(fieldName) || doc[fieldName].IsBsonNull)
        {
            return null;
        }

        var value = doc[fieldName];

        if (value.IsValidDateTime)
        {
            return value.ToUniversalTime();
        }

        return DateTime.TryParse(value.ToString(), out var result)
            ? result.ToUniversalTime()
            : null;
    }
}