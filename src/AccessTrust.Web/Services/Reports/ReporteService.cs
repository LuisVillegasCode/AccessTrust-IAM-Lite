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

    public async Task<ReporteDetallePaginadoViewModel<SolicitudDetalleReporteViewModel>> ListarSolicitudesDetalleAsync(
    ReportesFiltroViewModel filtro,
    int pagina,
    int tamanoPagina)
    {
        filtro ??= new ReportesFiltroViewModel();

        var (paginaNormalizada, tamanoNormalizado, skip) =
            NormalizarPaginacion(pagina, tamanoPagina);

        var match = ConstruirMatchOperativo(
            filtro,
            fechaCampo: "CreatedAt",
            incluirUsuario: true,
            incluirRecurso: true,
            estadoFiltro: filtro.EstadoSolicitud
        );

        var total = await _solicitudes.CountDocumentsAsync(match);

        var pipeline = new[]
        {
            new BsonDocument("$match", match),
            CrearLookup(MongoCollections.Usuarios, "UsuarioId", "_id", "usuario"),
            CrearUnwind("usuario"),
            CrearLookup(MongoCollections.Recursos, "RecursoId", "_id", "recurso"),
            CrearUnwind("recurso"),
            CrearLookup(MongoCollections.Usuarios, "AprobadorId", "_id", "aprobador"),
            CrearUnwind("aprobador"),
            new BsonDocument("$sort", new BsonDocument("CreatedAt", -1)),
            new BsonDocument("$skip", skip),
            new BsonDocument("$limit", tamanoNormalizado)
        };

        var registros = await _solicitudes
            .Aggregate<BsonDocument>(pipeline)
            .ToListAsync();

        await CargarOpcionesFiltroAsync(filtro);

        return new ReporteDetallePaginadoViewModel<SolicitudDetalleReporteViewModel>
        {
            Titulo = "Detalle de solicitudes de acceso",
            Descripcion = "Listado de solicitudes resultantes de los filtros aplicados.",
            SeccionOrigen = "Solicitudes",
            Filtro = filtro,
            Registros = registros.Select(MapearSolicitudDetalle).ToList(),
            PaginaActual = paginaNormalizada,
            TamanoPagina = tamanoNormalizado,
            TotalRegistros = total
        };
    }

    public async Task<ReporteDetallePaginadoViewModel<CredencialDetalleReporteViewModel>> ListarCredencialesDetalleAsync(
        ReportesFiltroViewModel filtro,
        int pagina,
        int tamanoPagina)
    {
        filtro ??= new ReportesFiltroViewModel();

        var (paginaNormalizada, tamanoNormalizado, skip) =
            NormalizarPaginacion(pagina, tamanoPagina);

        var match = ConstruirMatchOperativo(
            filtro,
            fechaCampo: "IssuedAt",
            incluirUsuario: true,
            incluirRecurso: true,
            estadoFiltro: filtro.EstadoCredencial
        );

        var total = await _credenciales.CountDocumentsAsync(match);

        var pipeline = new[]
        {
            new BsonDocument("$match", match),
            CrearLookup(MongoCollections.Usuarios, "UsuarioId", "_id", "usuario"),
            CrearUnwind("usuario"),
            CrearLookup(MongoCollections.Recursos, "RecursoId", "_id", "recurso"),
            CrearUnwind("recurso"),
            CrearLookup(MongoCollections.Usuarios, "EmitidaPorId", "_id", "emisor"),
            CrearUnwind("emisor"),
            new BsonDocument("$sort", new BsonDocument("IssuedAt", -1)),
            new BsonDocument("$skip", skip),
            new BsonDocument("$limit", tamanoNormalizado)
        };

        var registros = await _credenciales
            .Aggregate<BsonDocument>(pipeline)
            .ToListAsync();

        await CargarOpcionesFiltroAsync(filtro);

        return new ReporteDetallePaginadoViewModel<CredencialDetalleReporteViewModel>
        {
            Titulo = "Detalle de credenciales temporales",
            Descripcion = "Listado de credenciales temporales emitidas por el IAM.",
            SeccionOrigen = "Credenciales",
            Filtro = filtro,
            Registros = registros.Select(MapearCredencialDetalle).ToList(),
            PaginaActual = paginaNormalizada,
            TamanoPagina = tamanoNormalizado,
            TotalRegistros = total
        };
    }

    public async Task<ReporteDetallePaginadoViewModel<TicketDetalleReporteViewModel>> ListarTicketsDetalleAsync(
        ReportesFiltroViewModel filtro,
        int pagina,
        int tamanoPagina)
    {
        filtro ??= new ReportesFiltroViewModel();

        var (paginaNormalizada, tamanoNormalizado, skip) =
            NormalizarPaginacion(pagina, tamanoPagina);

        var match = ConstruirMatchOperativo(
            filtro,
            fechaCampo: "IssuedAt",
            incluirUsuario: true,
            incluirRecurso: true,
            estadoFiltro: filtro.EstadoTicket
        );

        var total = await _tickets.CountDocumentsAsync(match);

        var pipeline = new[]
        {
            new BsonDocument("$match", match),
            CrearLookup(MongoCollections.Usuarios, "UsuarioId", "_id", "usuario"),
            CrearUnwind("usuario"),
            CrearLookup(MongoCollections.Recursos, "RecursoId", "_id", "recurso"),
            CrearUnwind("recurso"),
            new BsonDocument("$sort", new BsonDocument("IssuedAt", -1)),
            new BsonDocument("$skip", skip),
            new BsonDocument("$limit", tamanoNormalizado)
        };

        var registros = await _tickets
            .Aggregate<BsonDocument>(pipeline)
            .ToListAsync();

        await CargarOpcionesFiltroAsync(filtro);

        return new ReporteDetallePaginadoViewModel<TicketDetalleReporteViewModel>
        {
            Titulo = "Detalle de tickets externos",
            Descripcion = "Listado de tickets emitidos para acceder a recursos externos protegidos.",
            SeccionOrigen = "Tickets",
            Filtro = filtro,
            Registros = registros.Select(MapearTicketDetalle).ToList(),
            PaginaActual = paginaNormalizada,
            TamanoPagina = tamanoNormalizado,
            TotalRegistros = total
        };
    }

    public async Task<ReporteDetallePaginadoViewModel<EventoAuditoriaDetalleReporteViewModel>> ListarAuditoriaDetalleAsync(
        ReportesFiltroViewModel filtro,
        int pagina,
        int tamanoPagina)
    {
        filtro ??= new ReportesFiltroViewModel();

        var (paginaNormalizada, tamanoNormalizado, skip) =
            NormalizarPaginacion(pagina, tamanoPagina);

        var match = ConstruirMatchAuditoria(
            filtro,
            incluirAccion: true
        );

        var total = await _eventosAuditoria.CountDocumentsAsync(match);

        var pipeline = new[]
        {
            new BsonDocument("$match", match),
            CrearLookup(MongoCollections.Usuarios, "ActorUserId", "_id", "actor"),
            CrearUnwind("actor"),
            new BsonDocument("$sort", new BsonDocument("Seq", -1)),
            new BsonDocument("$skip", skip),
            new BsonDocument("$limit", tamanoNormalizado)
        };

        var registros = await _eventosAuditoria
            .Aggregate<BsonDocument>(pipeline)
            .ToListAsync();

        await CargarOpcionesFiltroAsync(filtro);

        return new ReporteDetallePaginadoViewModel<EventoAuditoriaDetalleReporteViewModel>
        {
            Titulo = "Detalle de eventos de auditoría",
            Descripcion = "Listado de eventos registrados en la cadena de auditoría del IAM.",
            SeccionOrigen = "Auditoria",
            Filtro = filtro,
            Registros = registros.Select(MapearEventoAuditoriaDetalle).ToList(),
            PaginaActual = paginaNormalizada,
            TamanoPagina = tamanoNormalizado,
            TotalRegistros = total
        };
    }

    public async Task<ReporteDetallePaginadoViewModel<RecursoDetalleReporteViewModel>> ListarRecursosDetalleAsync(
        ReportesFiltroViewModel filtro,
        int pagina,
        int tamanoPagina)
    {
        filtro ??= new ReportesFiltroViewModel();

        var (paginaNormalizada, tamanoNormalizado, skip) =
            NormalizarPaginacion(pagina, tamanoPagina);

        var match = new BsonDocument();

        if (!string.IsNullOrWhiteSpace(filtro.RecursoId) &&
            ObjectId.TryParse(filtro.RecursoId, out var recursoId))
        {
            match["_id"] = recursoId;
        }

        if (!string.IsNullOrWhiteSpace(filtro.SensibilidadRecurso))
        {
            match["Sensibilidad"] = filtro.SensibilidadRecurso.Trim();
        }

        if (filtro.FechaDesde.HasValue || filtro.FechaHasta.HasValue)
        {
            match["CreatedAt"] = ConstruirFiltroFecha(filtro);
        }

        var total = await _recursos.CountDocumentsAsync(match);

        var pipeline = new[]
        {
            new BsonDocument("$match", match),
            CrearLookup(MongoCollections.Usuarios, "ResponsableId", "_id", "responsable"),
            CrearUnwind("responsable"),
            new BsonDocument("$sort", new BsonDocument("Nombre", 1)),
            new BsonDocument("$skip", skip),
            new BsonDocument("$limit", tamanoNormalizado)
        };

        var registros = await _recursos
            .Aggregate<BsonDocument>(pipeline)
            .ToListAsync();

        await CargarOpcionesFiltroAsync(filtro);

        return new ReporteDetallePaginadoViewModel<RecursoDetalleReporteViewModel>
        {
            Titulo = "Detalle de recursos protegidos",
            Descripcion = "Inventario de recursos registrados y protegidos por el IAM.",
            SeccionOrigen = "Recursos",
            Filtro = filtro,
            Registros = registros.Select(MapearRecursoDetalle).ToList(),
            PaginaActual = paginaNormalizada,
            TamanoPagina = tamanoNormalizado,
            TotalRegistros = total
        };
    }
    private async Task<List<ReporteConteoPorEstadoViewModel>> ObtenerSolicitudesPorEstadoAsync(
        ReportesFiltroViewModel filtro)
    {
        var match = ConstruirMatchOperativo(
            filtro,
            fechaCampo: "CreatedAt",
            incluirUsuario: true,
            incluirRecurso: true,
            estadoFiltro: filtro.EstadoSolicitud
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
            estadoFiltro: filtro.EstadoSolicitud
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
            estadoFiltro: filtro.EstadoCredencial
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
            estadoFiltro: filtro.EstadoCredencial
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
            estadoFiltro: filtro.EstadoTicket
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
        var match = ConstruirMatchAuditoria(
            filtro,
            incluirAccion: true
        );

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
        var match = ConstruirMatchAuditoria(
            filtro,
            incluirAccion: false
        );

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

        if (!string.IsNullOrWhiteSpace(filtro.SensibilidadRecurso))
        {
            match["Sensibilidad"] = filtro.SensibilidadRecurso.Trim();
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

        filtro.EstadosSolicitudDisponibles = new List<string>
        {
            "Pendiente",
            "Aprobada",
            "Rechazada"
        };

        filtro.EstadosCredencialDisponibles = new List<string>
        {
            "Activa",
            "Expirada",
            "Revocada",
            "Usada"
        };

        filtro.EstadosTicketDisponibles = new List<string>
        {
            "Activo",
            "Usado",
            "Expirado",
            "Revocado"
        };

        filtro.ResultadosAuditoriaDisponibles = new List<string>
        {
            "Exitoso",
            "Fallido",
            "Permitido",
            "Denegado"
        };

        filtro.SensibilidadesRecursoDisponibles = new List<string>
        {
            "Baja",
            "Media",
            "Alta"
        };

        // Compatibilidad temporal con la vista actual.
        // Se eliminará cuando actualicemos Index.cshtml.
        filtro.EstadosDisponibles = new List<string>();
        filtro.EstadosDisponibles.AddRange(filtro.EstadosSolicitudDisponibles);
        filtro.EstadosDisponibles.AddRange(filtro.EstadosCredencialDisponibles);
        filtro.EstadosDisponibles.AddRange(filtro.EstadosTicketDisponibles);
        filtro.EstadosDisponibles.AddRange(filtro.ResultadosAuditoriaDisponibles);

    }

    private static (int pagina, int tamanoPagina, int skip) NormalizarPaginacion(
        int pagina,
        int tamanoPagina)
    {
        var paginaNormalizada = Math.Max(1, pagina);
        var tamanoNormalizado = Math.Clamp(tamanoPagina, 10, 50);
        var skip = (paginaNormalizada - 1) * tamanoNormalizado;

        return (paginaNormalizada, tamanoNormalizado, skip);
    }

    private static BsonDocument CrearLookup(
        string from,
        string localField,
        string foreignField,
        string alias)
    {
        return new BsonDocument("$lookup", new BsonDocument
        {
            { "from", from },
            { "localField", localField },
            { "foreignField", foreignField },
            { "as", alias }
        });
    }

    private static BsonDocument CrearUnwind(string path)
    {
        return new BsonDocument("$unwind", new BsonDocument
        {
            { "path", $"${path}" },
            { "preserveNullAndEmptyArrays", true }
        });
    }

    private static BsonDocument GetSubdocument(
        BsonDocument document,
        string fieldName)
    {
        if (!document.TryGetValue(fieldName, out var value) ||
            value.IsBsonNull ||
            !value.IsBsonDocument)
        {
            return new BsonDocument();
        }

        return value.AsBsonDocument;
    }

    private static DateTime? GetBsonDate(
        BsonDocument document,
        string fieldName)
    {
        if (!document.TryGetValue(fieldName, out var value) || value.IsBsonNull)
        {
            return null;
        }

        if (value.IsValidDateTime)
        {
            return value.ToUniversalTime();
        }

        if (value.IsString &&
            DateTime.TryParse(value.AsString, out var fecha))
        {
            return fecha;
        }

        return null;
    }

    private static int GetBsonInt(
        BsonDocument document,
        string fieldName,
        int defaultValue = 0)
    {
        if (!document.TryGetValue(fieldName, out var value) || value.IsBsonNull)
        {
            return defaultValue;
        }

        if (value.IsInt32)
        {
            return value.AsInt32;
        }

        if (value.IsInt64)
        {
            return Convert.ToInt32(value.AsInt64);
        }

        if (value.IsDouble)
        {
            return Convert.ToInt32(value.AsDouble);
        }

        return defaultValue;
    }

    private static long GetBsonLong(
        BsonDocument document,
        string fieldName,
        long defaultValue = 0)
    {
        if (!document.TryGetValue(fieldName, out var value) || value.IsBsonNull)
        {
            return defaultValue;
        }

        if (value.IsInt64)
        {
            return value.AsInt64;
        }

        if (value.IsInt32)
        {
            return value.AsInt32;
        }

        return defaultValue;
    }

    private static bool GetBsonBool(
        BsonDocument document,
        string fieldName,
        bool defaultValue = false)
    {
        if (!document.TryGetValue(fieldName, out var value) || value.IsBsonNull)
        {
            return defaultValue;
        }

        return value.IsBoolean
            ? value.AsBoolean
            : defaultValue;
    }

    private static string ResumirDetalle(BsonDocument document)
    {
        if (!document.TryGetValue("Detalle", out var detalle) || detalle.IsBsonNull)
        {
            return string.Empty;
        }

        var texto = detalle.ToString() ?? string.Empty;

        return texto.Length <= 120
            ? texto
            : $"{texto[..120]}...";
    }

    private static SolicitudDetalleReporteViewModel MapearSolicitudDetalle(
    BsonDocument document)
    {
        var usuario = GetSubdocument(document, "usuario");
        var recurso = GetSubdocument(document, "recurso");
        var aprobador = GetSubdocument(document, "aprobador");

        return new SolicitudDetalleReporteViewModel
        {
            Id = GetBsonValueAsString(document.GetValue("_id", string.Empty)),
            FechaCreacion = GetBsonDate(document, "CreatedAt"),
            UsuarioNombre = GetBsonString(usuario, "Nombre"),
            UsuarioCorreo = GetBsonString(usuario, "Correo"),
            RecursoNombre = GetBsonString(recurso, "Nombre"),
            RecursoTipo = GetBsonString(recurso, "Tipo"),
            Sensibilidad = GetBsonString(recurso, "Sensibilidad"),
            Estado = GetBsonString(document, "Estado"),
            Motivo = GetBsonString(document, "Motivo"),
            AprobadorNombre = GetBsonString(aprobador, "Nombre"),
            FechaDecision = GetBsonDate(document, "DecisionAt")
        };
    }

    private static CredencialDetalleReporteViewModel MapearCredencialDetalle(
        BsonDocument document)
    {
        var usuario = GetSubdocument(document, "usuario");
        var recurso = GetSubdocument(document, "recurso");
        var emisor = GetSubdocument(document, "emisor");

        return new CredencialDetalleReporteViewModel
        {
            Id = GetBsonValueAsString(document.GetValue("_id", string.Empty)),
            FechaEmision = GetBsonDate(document, "IssuedAt"),
            FechaExpiracion = GetBsonDate(document, "ExpiresAt"),
            UsuarioNombre = GetBsonString(usuario, "Nombre"),
            UsuarioCorreo = GetBsonString(usuario, "Correo"),
            RecursoNombre = GetBsonString(recurso, "Nombre"),
            RecursoTipo = GetBsonString(recurso, "Tipo"),
            Sensibilidad = GetBsonString(recurso, "Sensibilidad"),
            Estado = GetBsonString(document, "Estado"),
            UsosRealizados = GetBsonInt(document, "UsosRealizados"),
            UsosMaximos = GetBsonInt(document, "UsosMaximos"),
            EmitidaPorNombre = GetBsonString(emisor, "Nombre")
        };
    }

    private static TicketDetalleReporteViewModel MapearTicketDetalle(
        BsonDocument document)
    {
        var usuario = GetSubdocument(document, "usuario");
        var recurso = GetSubdocument(document, "recurso");

        var credencialId = document.Contains("CredencialId")
            ? document.GetValue("CredencialId", string.Empty)
            : document.GetValue("CredencialTemporalId", string.Empty);

        return new TicketDetalleReporteViewModel
        {
            Id = GetBsonValueAsString(document.GetValue("_id", string.Empty)),
            FechaEmision = GetBsonDate(document, "IssuedAt"),
            FechaExpiracion = GetBsonDate(document, "ExpiresAt"),
            FechaConsumo = GetBsonDate(document, "ConsumedAt"),
            UsuarioNombre = GetBsonString(usuario, "Nombre"),
            UsuarioCorreo = GetBsonString(usuario, "Correo"),
            RecursoNombre = GetBsonString(recurso, "Nombre"),
            RecursoTipo = GetBsonString(recurso, "Tipo"),
            Sensibilidad = GetBsonString(recurso, "Sensibilidad"),
            Estado = GetBsonString(document, "Estado"),
            CredencialId = GetBsonValueAsString(credencialId)
        };
    }

    private static EventoAuditoriaDetalleReporteViewModel MapearEventoAuditoriaDetalle(
        BsonDocument document)
    {
        var actor = GetSubdocument(document, "actor");

        return new EventoAuditoriaDetalleReporteViewModel
        {
            Id = GetBsonValueAsString(document.GetValue("_id", string.Empty)),
            Seq = GetBsonLong(document, "Seq"),
            FechaCreacion = GetBsonDate(document, "CreatedAt"),
            ActorNombre = GetBsonString(actor, "Nombre"),
            ActorCorreo = GetBsonString(actor, "Correo"),
            Accion = GetBsonString(document, "Accion"),
            Resultado = GetBsonString(document, "Resultado"),
            DetalleResumen = ResumirDetalle(document),
            PrevHash = GetBsonString(document, "PrevHash"),
            Hash = GetBsonString(document, "Hash")
        };
    }

    private static RecursoDetalleReporteViewModel MapearRecursoDetalle(
        BsonDocument document)
    {
        var responsable = GetSubdocument(document, "responsable");

        return new RecursoDetalleReporteViewModel
        {
            Id = GetBsonValueAsString(document.GetValue("_id", string.Empty)),
            Nombre = GetBsonString(document, "Nombre"),
            Tipo = GetBsonString(document, "Tipo"),
            Sensibilidad = GetBsonString(document, "Sensibilidad"),
            Url = GetBsonString(document, "Url"),
            ResponsableNombre = GetBsonString(responsable, "Nombre"),
            ResponsableCorreo = GetBsonString(responsable, "Correo"),
            Activo = GetBsonBool(document, "Activo"),
            FechaCreacion = GetBsonDate(document, "CreatedAt")
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
        string? estadoFiltro)
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

        if (!string.IsNullOrWhiteSpace(estadoFiltro))
        {
            match["Estado"] = estadoFiltro.Trim();
        }

        return match;
    }

    private static BsonDocument ConstruirMatchAuditoria(
        ReportesFiltroViewModel filtro,
        bool incluirAccion)
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

        if (!string.IsNullOrWhiteSpace(filtro.ResultadoAuditoria) &&
            ResultadosAuditoria.Contains(filtro.ResultadoAuditoria.Trim()))
        {
            match["Resultado"] = filtro.ResultadoAuditoria.Trim();
        }

        if (incluirAccion && !string.IsNullOrWhiteSpace(filtro.AccionAuditoria))
        {
            var patron = Regex.Escape(filtro.AccionAuditoria.Trim());

            match["Accion"] = new BsonRegularExpression(patron, "i");
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