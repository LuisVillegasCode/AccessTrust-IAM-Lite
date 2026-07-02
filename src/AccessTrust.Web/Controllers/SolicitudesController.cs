using System.Security.Claims;
using AccessTrust.Web.Models;
using AccessTrust.Web.Services.Requests;
using AccessTrust.Web.Services.Resources;
using AccessTrust.Web.ViewModels.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AccessTrust.Web.Controllers;

[Authorize(Roles = "Solicitante")]
public class SolicitudesController : Controller
{
    private readonly ISolicitudAccesoService _solicitudAccesoService;
    private readonly IRecursoService _recursoService;

    public SolicitudesController(
        ISolicitudAccesoService solicitudAccesoService,
        IRecursoService recursoService)
    {
        _solicitudAccesoService = solicitudAccesoService;
        _recursoService = recursoService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var usuarioId = ObtenerUsuarioId();

        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return RedirectToAction("Login", "Account");
        }

        var solicitudes = await _solicitudAccesoService.GetByUsuarioAsync(usuarioId);

        var viewModels = new List<SolicitudAccesoListItemViewModel>();

        foreach (var solicitud in solicitudes)
        {
            var recurso = await _recursoService.GetByIdAsync(solicitud.RecursoId);

            viewModels.Add(new SolicitudAccesoListItemViewModel
            {
                Id = solicitud.Id ?? string.Empty,
                RecursoNombre = recurso?.Nombre ?? "Recurso no encontrado",
                RecursoSensibilidad = recurso?.Sensibilidad ?? SensibilidadRecurso.Baja,
                Motivo = solicitud.Motivo,
                DuracionSolicitadaMin = solicitud.DuracionSolicitadaMin,
                Prioridad = solicitud.Prioridad,
                Estado = solicitud.Estado,
                CreatedAt = solicitud.CreatedAt,
                ResolvedAt = solicitud.ResolvedAt
            });
        }

        return View(viewModels);
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        var usuarioId = ObtenerUsuarioId();

        if (string.IsNullOrWhiteSpace(usuarioId) || string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var solicitud = await _solicitudAccesoService.GetByIdAndUsuarioAsync(id, usuarioId);

        if (solicitud is null)
        {
            return NotFound();
        }

        var recurso = await _recursoService.GetByIdAsync(solicitud.RecursoId);

        if (recurso is null)
        {
            return NotFound();
        }

        var viewModel = new SolicitudAccesoDetailsViewModel
        {
            Id = solicitud.Id ?? string.Empty,
            UsuarioId = solicitud.UsuarioId,
            RecursoId = solicitud.RecursoId,
            RecursoNombre = recurso.Nombre,
            RecursoTipo = recurso.Tipo,
            RecursoSensibilidad = recurso.Sensibilidad,
            Motivo = solicitud.Motivo,
            DuracionSolicitadaMin = solicitud.DuracionSolicitadaMin,
            Prioridad = solicitud.Prioridad,
            Estado = solicitud.Estado,
            AprobadorId = solicitud.AprobadorId,
            Observacion = solicitud.Observacion,
            CreatedAt = solicitud.CreatedAt,
            ResolvedAt = solicitud.ResolvedAt
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var viewModel = new CrearSolicitudAccesoViewModel();
        await CargarRecursosDisponiblesAsync(viewModel);

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CrearSolicitudAccesoViewModel viewModel)
    {
        var usuarioId = ObtenerUsuarioId();

        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return RedirectToAction("Login", "Account");
        }

        if (!ModelState.IsValid)
        {
            await CargarRecursosDisponiblesAsync(viewModel);
            return View(viewModel);
        }

        var solicitud = new SolicitudAcceso
        {
            UsuarioId = usuarioId,
            RecursoId = viewModel.RecursoId,
            Motivo = viewModel.Motivo,
            DuracionSolicitadaMin = viewModel.DuracionSolicitadaMin,
            Prioridad = viewModel.Prioridad,
            Estado = EstadoSolicitud.Pendiente,
            CreatedAt = DateTime.UtcNow
        };

        var resultado = await _solicitudAccesoService.CreateAsync(solicitud);

        if (!resultado.Success)
        {
            ModelState.AddModelError(string.Empty, resultado.Message);
            await CargarRecursosDisponiblesAsync(viewModel);
            return View(viewModel);
        }

        return RedirectToAction(nameof(Index));
    }

    private string? ObtenerUsuarioId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private async Task CargarRecursosDisponiblesAsync(CrearSolicitudAccesoViewModel viewModel)
    {
        var recursos = await _recursoService.GetActivosAsync();

        viewModel.RecursosDisponibles = recursos
            .Select(r => new SelectListItem
            {
                Value = r.Id,
                Text = $"{r.Nombre} - {r.Sensibilidad}"
            })
            .ToList();
    }
}