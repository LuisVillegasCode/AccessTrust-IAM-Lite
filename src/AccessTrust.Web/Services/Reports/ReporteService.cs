using System.Text.RegularExpressions;
using AccessTrust.Web.Data;
using AccessTrust.Web.ViewModels.Reports;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AccessTrust.Web.Services.Reports;

public class ReporteService : IReporteService
{

    public async Task<List<ReporteFiltroOpcionViewModel>> BuscarUsuariosAsync(
        string term,
        int limite = 10)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return new List<ReporteFiltroOpcionViewModel>();
        }

        var termino = term.Trim();

        if (termino.Length < 2)
        {
            return new List<ReporteFiltroOpcionViewModel>();
        }

        var limiteSeguro = Math.Clamp(limite, 1, 10);
        var patron = Regex.Escape(termino);

        var filtro = Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Regex(
                "Nombre",
                new BsonRegularExpression(patron, "i")
            ),
            Builders<BsonDocument>.Filter.Regex(
                "Correo",
                new BsonRegularExpression(patron, "i")
            )
        );

        var usuarios = await _usuarios
            .Find(filtro)
            .Sort(new BsonDocument("Nombre", 1))
            .Limit(limiteSeguro)
            .ToListAsync();

        return usuarios.Select(u => new ReporteFiltroOpcionViewModel
        {
            Id = GetBsonValueAsString(u.GetValue("_id")),
            Texto = $"{GetBsonString(u, "Nombre")} - {GetBsonString(u, "Correo")}"
        }).ToList();
    }
    private readonly IMongoCollection<BsonDocument> _solicitudes;
    private readonly IMongoCollection<BsonDocument> _credenciales;
    private readonly IMongoCollection<BsonDocument> _tickets;
    private readonly IMongoCollection<BsonDocument> _eventosAuditoria;
    private readonly IMongoCollection<BsonDocument> _recursos;
    private readonly IMongoCollection<BsonDocument> _usuarios;

    private static readonly HashSet<string> ResultadosAuditoria = new(
        new[] { "Exitoso", "Fallido", "Permitido", "Denegado" },
        StringComparer.OrdinalIgnoreCase
    );

    public ReporteService(IMongoDatabase database)
    {
        _solicitudes = database.GetCollection<BsonDocument>(
            MongoCollections.SolicitudesAcceso
        );

        _credenciales = database.GetCollection<BsonDocument>(
            MongoCollections.CredencialesTemporales
        );

        _tickets = database.GetCollection<BsonDocument>(
            MongoCollections.TicketsAccesoExterno
        );

        _eventosAuditoria = database.GetCollection<BsonDocument>(
            MongoCollections.EventosAuditoria
        );

        _recursos = database.GetCollection<BsonDocument>(
            MongoCollections.Recursos
        );

        _usuarios = database.GetCollection<BsonDocument>(
            MongoCollections.Usuarios
        );
    }

    public async Task<ReportesDashboardViewModel> GenerarDashboardAsync(
        ReportesFiltroViewModel filtro)
    {
        filtro ??= new ReportesFiltroViewModel();

        var dashboard = new ReportesDashboardViewModel
        {
            GeneradoAt = DateTime.UtcNow,
            Filtro = filtro
        };

        await CargarOpcionesFiltroAsync(dashboard.Filtro);

        dashboard.SolicitudesPorEstado =
            await ObtenerSolicitudesPorEstadoAsync(filtro);

        dashboard.SolicitudesPorRecurso =
            await ObtenerSolicitudesPorRecursoAsync(filtro);

        dashboard.CredencialesPorEstado =
            await ObtenerCredencialesPorEstadoAsync(filtro);

        dashboard.CredencialesPorRecurso =
            await ObtenerCredencialesPorRecursoAsync(filtro);

        dashboard.TicketsExternosPorEstado =
            await ObtenerTicketsExternosPorEstadoAsync(filtro);

        dashboard.EventosAuditoriaPorAccionResultado =
            await ObtenerEventosAuditoriaPorAccionResultadoAsync(filtro);

        dashboard.AccesosExternosPermitidosDenegados =
            await ObtenerAccesosExternosPermitidosDenegadosAsync(filtro);

        dashboard.RecursosPorSensibilidad =
            await ObtenerRecursosPorSensibilidadAsync(filtro);

        return dashboard;
    }

    private async Task<List<ReporteConteoPorEstadoViewModel>> ObtenerSolicitudesPorEstadoAsync(
        ReportesFiltroViewModel filtro)
    {
        var match = ConstruirMatchOperativo(
            filtro,
            fechaCampo: "CreatedAt",
            incluirUsuario: true,
            incluirRecurso: true,
            incluirEstado: true
        );

        var pipeline = new[]
        {
            new BsonDocument("$match", match),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$Estado" },
                { "Total", new BsonDocument("$sum", 1) }
            }),
            new BsonDocument("$sort", new BsonDocument("_id", 1))
        };

        var resultados = await _solicitudes
            .Aggregate<BsonDocument>(pipeline)
            .ToListAsync();

        return resultados.Select(r => new ReporteConteoPorEstadoViewModel
        {
            Estado = GetBsonValueAsString(r.GetValue("_id", string.Empty)),
            Total = GetTotal(r)
        }).ToList();
    }

    private async Task<List<ReporteConteoPorRecursoViewModel>> ObtenerSolicitudesPorRecursoAsync(
        ReportesFiltroViewModel filtro)
    {
        var match = ConstruirMatchOperativo(
            filtro,
            fechaCampo: "CreatedAt",
            incluirUsuario: true,
            incluirRecurso: true,
            incluirEstado: true
        );

        var pipeline = ConstruirPipelineConteoPorRecurso(match);

        var resultados = await _solicitudes
            .Aggregate<BsonDocument>(pipeline)
            .ToListAsync();

        return MapearConteoPorRecurso(resultados);
    }

    private async Task<List<ReporteConteoPorEstadoViewModel>> ObtenerCredencialesPorEstadoAsync(
        ReportesFiltroViewModel filtro)
    {
        var match = ConstruirMatchOperativo(
            filtro,
            fechaCampo: "IssuedAt",
            incluirUsuario: true,
            incluirRecurso: true,
            incluirEstado: true
        );

        var pipeline = new[]
        {
            new BsonDocument("$match", match),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$Estado" },
                { "Total", new BsonDocument("$sum", 1) }
            }),
            new BsonDocument("$sort", new BsonDocument("_id", 1))
        };

        var resultados = await _credenciales
            .Aggregate<BsonDocument>(pipeline)
            .ToListAsync();

        return resultados.Select(r => new ReporteConteoPorEstadoViewModel
        {
            Estado = GetBsonValueAsString(r.GetValue("_id", string.Empty)),
            Total = GetTotal(r)
        }).ToList();
    }

    private async Task<List<ReporteConteoPorRecursoViewModel>> ObtenerCredencialesPorRecursoAsync(
        ReportesFiltroViewModel filtro)
    {
        var match = ConstruirMatchOperativo(
            filtro,
            fechaCampo: "IssuedAt",
            incluirUsuario: true,
            incluirRecurso: true,
            incluirEstado: true
        );

        var pipeline = ConstruirPipelineConteoPorRecurso(match);

        var resultados = await _credenciales
            .Aggregate<BsonDocument>(pipeline)
            .ToListAsync();

        return MapearConteoPorRecurso(resultados);
    }

    private async Task<List<ReporteConteoPorEstadoViewModel>> ObtenerTicketsExternosPorEstadoAsync(
        ReportesFiltroViewModel filtro)
    {
        var match = ConstruirMatchOperativo(
            filtro,
            fechaCampo: "IssuedAt",
            incluirUsuario: true,
            incluirRecurso: true,
            incluirEstado: true
        );

        var pipeline = new[]
        {
            new BsonDocument("$match", match),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$Estado" },
                { "Total", new BsonDocument("$sum", 1) }
            }),
            new BsonDocument("$sort", new BsonDocument("_id", 1))
        };

        var resultados = await _tickets
            .Aggregate<BsonDocument>(pipeline)
            .ToListAsync();

        return resultados.Select(r => new ReporteConteoPorEstadoViewModel
        {
            Estado = GetBsonValueAsString(r.GetValue("_id", string.Empty)),
            Total = GetTotal(r)
        }).ToList();
    }

    private async Task<List<ReporteAuditoriaAccionResultadoViewModel>> ObtenerEventosAuditoriaPorAccionResultadoAsync(
        ReportesFiltroViewModel filtro)
    {
        var match = ConstruirMatchAuditoria(filtro);

        var pipeline = new[]
        {
            new BsonDocument("$match", match),
            new BsonDocument("$group", new BsonDocument
            {
                {
                    "_id",
                    new BsonDocument
                    {
                        { "Accion", "$Accion" },
                        { "Resultado", "$Resultado" }
                    }
                },
                { "Total", new BsonDocument("$sum", 1) }
            }),
            new BsonDocument("$sort", new BsonDocument("Total", -1))
        };

        var resultados = await _eventosAuditoria
            .Aggregate<BsonDocument>(pipeline)
            .ToListAsync();

        return resultados.Select(r =>
        {
            var id = r.GetValue("_id").AsBsonDocument;

            return new ReporteAuditoriaAccionResultadoViewModel
            {
                Accion = GetBsonString(id, "Accion"),
                Resultado = GetBsonString(id, "Resultado"),
                Total = GetTotal(r)
            };
        }).ToList();
    }

    private async Task<List<ReporteAccesoExternoViewModel>> ObtenerAccesosExternosPermitidosDenegadosAsync(
        ReportesFiltroViewModel filtro)
    {
        var match = ConstruirMatchAuditoria(filtro);

        match["Accion"] = new BsonDocument("$in", new BsonArray
        {
            "ACCESO_EXTERNO_PERMITIDO",
            "TICKET_EXTERNO_DENEGADO",
            "TICKET_EXTERNO_VACIO",
            "TICKET_EXTERNO_INVALIDO",
            "TICKET_RECURSO_INVALIDO",
            "TICKET_EXTERNO_NO_ACTIVO",
            "TICKET_EXTERNO_EXPIRADO"
        });

        var pipeline = new[]
        {
            new BsonDocument("$match", match),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$Resultado" },
                { "Total", new BsonDocument("$sum", 1) }
            }),
            new BsonDocument("$sort", new BsonDocument("_id", 1))
        };

        var resultados = await _eventosAuditoria
            .Aggregate<BsonDocument>(pipeline)
            .ToListAsync();

        return resultados.Select(r => new ReporteAccesoExternoViewModel
        {
            Resultado = GetBsonValueAsString(r.GetValue("_id", string.Empty)),
            Total = GetTotal(r)
        }).ToList();
    }

    private async Task<List<ReporteRecursoPorSensibilidadViewModel>> ObtenerRecursosPorSensibilidadAsync(
        ReportesFiltroViewModel filtro)
    {
        var match = new BsonDocument();

        if (!string.IsNullOrWhiteSpace(filtro.RecursoId) &&
            ObjectId.TryParse(filtro.RecursoId, out var recursoId))
        {
            match["_id"] = recursoId;
        }

        if (filtro.FechaDesde.HasValue || filtro.FechaHasta.HasValue)
        {
            match["CreatedAt"] = ConstruirFiltroFecha(filtro);
        }

        var pipeline = new[]
        {
            new BsonDocument("$match", match),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$Sensibilidad" },
                { "Total", new BsonDocument("$sum", 1) }
            }),
            new BsonDocument("$sort", new BsonDocument("_id", 1))
        };

        var resultados = await _recursos
            .Aggregate<BsonDocument>(pipeline)
            .ToListAsync();

        return resultados.Select(r => new ReporteRecursoPorSensibilidadViewModel
        {
            Sensibilidad = GetBsonValueAsString(r.GetValue("_id", string.Empty)),
            Total = GetTotal(r)
        }).ToList();
    }

    private async Task CargarOpcionesFiltroAsync(ReportesFiltroViewModel filtro)
    {
        var recursos = await _recursos
            .Find(Builders<BsonDocument>.Filter.Empty)
            .Sort(new BsonDocument("Nombre", 1))
            .ToListAsync();

        filtro.RecursosDisponibles = recursos.Select(r => new ReporteFiltroOpcionViewModel
        {
            Id = GetBsonValueAsString(r.GetValue("_id")),
            Texto = $"{GetBsonString(r, "Nombre")} ({GetBsonString(r, "Sensibilidad")})"
        }).ToList();

        filtro.UsuariosDisponibles = new List<ReporteFiltroOpcionViewModel>();
        filtro.UsuarioTexto = string.Empty;

        if (!string.IsNullOrWhiteSpace(filtro.UsuarioId) &&
            ObjectId.TryParse(filtro.UsuarioId, out var usuarioSeleccionadoId))
        {
            var usuarioSeleccionado = await _usuarios
                .Find(Builders<BsonDocument>.Filter.Eq("_id", usuarioSeleccionadoId))
                .FirstOrDefaultAsync();

            if (usuarioSeleccionado is not null)
            {
                filtro.UsuarioTexto =
                    $"{GetBsonString(usuarioSeleccionado, "Nombre")} - {GetBsonString(usuarioSeleccionado, "Correo")}";

                filtro.UsuariosDisponibles.Add(new ReporteFiltroOpcionViewModel
                {
                    Id = filtro.UsuarioId,
                    Texto = filtro.UsuarioTexto
                });
            }
        }

        filtro.EstadosDisponibles = new List<string>
        {
            "Pendiente",
            "Aprobada",
            "Rechazada",
            "Activa",
            "Expirada",
            "Revocada",
            "Usada",
            "Activo",
            "Usado",
            "Expirado",
            "Revocado",
            "Exitoso",
            "Fallido",
            "Permitido",
            "Denegado"
        };
    }

    private static BsonDocument[] ConstruirPipelineConteoPorRecurso(
        BsonDocument match)
    {
        return new[]
        {
            new BsonDocument("$match", match),
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", MongoCollections.Recursos },
                { "localField", "RecursoId" },
                { "foreignField", "_id" },
                { "as", "recurso" }
            }),
            new BsonDocument("$unwind", new BsonDocument
            {
                { "path", "$recurso" },
                { "preserveNullAndEmptyArrays", true }
            }),
            new BsonDocument("$group", new BsonDocument
            {
                {
                    "_id",
                    new BsonDocument
                    {
                        { "RecursoId", "$RecursoId" },
                        {
                            "RecursoNombre",
                            new BsonDocument("$ifNull", new BsonArray
                            {
                                "$recurso.Nombre",
                                "Recurso no encontrado"
                            })
                        },
                        {
                            "RecursoTipo",
                            new BsonDocument("$ifNull", new BsonArray
                            {
                                "$recurso.Tipo",
                                "No disponible"
                            })
                        },
                        {
                            "Sensibilidad",
                            new BsonDocument("$ifNull", new BsonArray
                            {
                                "$recurso.Sensibilidad",
                                "No disponible"
                            })
                        }
                    }
                },
                { "Total", new BsonDocument("$sum", 1) }
            }),
            new BsonDocument("$sort", new BsonDocument("Total", -1))
        };
    }

    private static List<ReporteConteoPorRecursoViewModel> MapearConteoPorRecurso(
        List<BsonDocument> resultados)
    {
        return resultados.Select(r =>
        {
            var id = r.GetValue("_id").AsBsonDocument;

            return new ReporteConteoPorRecursoViewModel
            {
                RecursoId = GetBsonValueAsString(id.GetValue("RecursoId", string.Empty)),
                RecursoNombre = GetBsonString(id, "RecursoNombre"),
                RecursoTipo = GetBsonString(id, "RecursoTipo"),
                Sensibilidad = GetBsonString(id, "Sensibilidad"),
                Total = GetTotal(r)
            };
        }).ToList();
    }

    private static BsonDocument ConstruirMatchOperativo(
        ReportesFiltroViewModel filtro,
        string fechaCampo,
        bool incluirUsuario,
        bool incluirRecurso,
        bool incluirEstado)
    {
        var match = new BsonDocument();

        if (filtro.FechaDesde.HasValue || filtro.FechaHasta.HasValue)
        {
            match[fechaCampo] = ConstruirFiltroFecha(filtro);
        }

        if (incluirUsuario &&
            !string.IsNullOrWhiteSpace(filtro.UsuarioId) &&
            ObjectId.TryParse(filtro.UsuarioId, out var usuarioId))
        {
            match["UsuarioId"] = usuarioId;
        }

        if (incluirRecurso &&
            !string.IsNullOrWhiteSpace(filtro.RecursoId) &&
            ObjectId.TryParse(filtro.RecursoId, out var recursoId))
        {
            match["RecursoId"] = recursoId;
        }

        if (incluirEstado && !string.IsNullOrWhiteSpace(filtro.Estado))
        {
            match["Estado"] = filtro.Estado.Trim();
        }

        return match;
    }

    private static BsonDocument ConstruirMatchAuditoria(
        ReportesFiltroViewModel filtro)
    {
        var match = new BsonDocument();

        if (filtro.FechaDesde.HasValue || filtro.FechaHasta.HasValue)
        {
            match["CreatedAt"] = ConstruirFiltroFecha(filtro);
        }

        if (!string.IsNullOrWhiteSpace(filtro.UsuarioId) &&
            ObjectId.TryParse(filtro.UsuarioId, out var usuarioId))
        {
            match["ActorUserId"] = usuarioId;
        }

        if (!string.IsNullOrWhiteSpace(filtro.RecursoId))
        {
            match["Detalle.recurso_id"] = filtro.RecursoId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(filtro.Estado) &&
            ResultadosAuditoria.Contains(filtro.Estado.Trim()))
        {
            match["Resultado"] = filtro.Estado.Trim();
        }

        return match;
    }

    private static BsonDocument ConstruirFiltroFecha(
        ReportesFiltroViewModel filtro)
    {
        var fecha = new BsonDocument();

        if (filtro.FechaDesde.HasValue)
        {
            fecha["$gte"] = NormalizarFechaUtc(filtro.FechaDesde.Value);
        }

        if (filtro.FechaHasta.HasValue)
        {
            fecha["$lte"] = NormalizarFechaUtc(filtro.FechaHasta.Value);
        }

        return fecha;
    }

    private static DateTime NormalizarFechaUtc(DateTime fecha)
    {
        return fecha.Kind == DateTimeKind.Utc
            ? fecha
            : fecha.ToUniversalTime();
    }

    private static string GetBsonString(
        BsonDocument document,
        string fieldName,
        string defaultValue = "")
    {
        if (!document.TryGetValue(fieldName, out var value) || value.IsBsonNull)
        {
            return defaultValue;
        }

        return GetBsonValueAsString(value);
    }

    private static string GetBsonValueAsString(BsonValue value)
    {
        if (value.IsBsonNull)
        {
            return string.Empty;
        }

        if (value.IsString)
        {
            return value.AsString;
        }

        if (value.IsObjectId)
        {
            return value.AsObjectId.ToString();
        }

        return value.ToString() ?? string.Empty;
    }

    private static int GetTotal(BsonDocument document)
    {
        var value = document.GetValue("Total", 0);

        return value.IsInt32
            ? value.AsInt32
            : Convert.ToInt32(value.AsInt64);
    }
}