using System.Security.Claims;
using AccessTrust.Web.Models;
using AccessTrust.Web.Services.Credentials;
using AccessTrust.Web.Services.Policies;
using AccessTrust.Web.Services.Requests;
using AccessTrust.Web.Services.Resources;
using AccessTrust.Web.ViewModels.Approvals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessTrust.Web.Controllers;

[Authorize(Roles = "Aprobador")]
public class AprobacionesController : Controller
{
    private readonly ISolicitudAccesoService _solicitudAccesoService;
    private readonly IRecursoService _recursoService;
    private readonly IPoliticaAccesoService _politicaAccesoService;
    private readonly ICredencialTemporalService _credencialTemporalService;
    private const string ExternalTokenCookieName = "AccessTrust.ExternalToken";

    public AprobacionesController(
        ISolicitudAccesoService solicitudAccesoService,
        IRecursoService recursoService,
        IPoliticaAccesoService politicaAccesoService,
        ICredencialTemporalService credencialTemporalService)
    {
        _solicitudAccesoService = solicitudAccesoService;
        _recursoService = recursoService;
        _politicaAccesoService = politicaAccesoService;
        _credencialTemporalService = credencialTemporalService;
    }

    public async Task<IActionResult> Index()
    {
        var solicitudes = await _solicitudAccesoService.GetPendientesAsync();
        var viewModels = new List<SolicitudPendienteListItemViewModel>();

        foreach (var solicitud in solicitudes)
        {
            var recurso = await _recursoService.GetByIdAsync(solicitud.RecursoId);

            viewModels.Add(new SolicitudPendienteListItemViewModel
            {
                Id = solicitud.Id ?? string.Empty,
                UsuarioId = solicitud.UsuarioId,
                RecursoId = solicitud.RecursoId,
                RecursoNombre = recurso?.Nombre ?? "Recurso no encontrado",
                RecursoTipo = recurso?.Tipo ?? "No disponible",
                RecursoSensibilidad = recurso?.Sensibilidad ?? SensibilidadRecurso.Baja,
                Motivo = solicitud.Motivo,
                DuracionSolicitadaMin = solicitud.DuracionSolicitadaMin,
                Prioridad = solicitud.Prioridad,
                Estado = solicitud.Estado,
                CreatedAt = solicitud.CreatedAt
            });
        }

        return View(viewModels);
    }

    public async Task<IActionResult> Details(string id)
    {
        var solicitud = await _solicitudAccesoService.GetByIdAsync(id);

        if (solicitud is null)
        {
            return NotFound();
        }

        if (solicitud.Estado != EstadoSolicitud.Pendiente)
        {
            TempData["Error"] = "La solicitud ya no se encuentra pendiente.";
            return RedirectToAction(nameof(Index));
        }

        var recurso = await _recursoService.GetByIdAsync(solicitud.RecursoId);

        if (recurso is null)
        {
            TempData["Error"] = "El recurso asociado a la solicitud no existe.";
            return RedirectToAction(nameof(Index));
        }

        var politica = await _politicaAccesoService.GetBySensibilidadAsync(recurso.Sensibilidad);

        if (politica is null)
        {
            TempData["Error"] = "No existe una política asociada a la sensibilidad del recurso.";
            return RedirectToAction(nameof(Index));
        }

        var duracionSugerida = Math.Min(
            solicitud.DuracionSolicitadaMin,
            politica.DuracionMaxMin
        );

        var viewModel = new SolicitudAprobacionDetailsViewModel
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
            CreatedAt = solicitud.CreatedAt,
            PoliticaNombre = politica.Nombre,
            PoliticaDuracionMaxMin = politica.DuracionMaxMin,
            PoliticaRequiereOtp = politica.RequiereOtp,
            PoliticaMaxUsos = politica.MaxUsos,
            DuracionAprobadaMin = duracionSugerida,
            Observacion = string.Empty
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Aprobar(AprobarSolicitudViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Los datos enviados para aprobar la solicitud no son válidos.";
            return RedirectToAction(nameof(Details), new { id = model.SolicitudId });
        }

        var aprobadorId = ObtenerUsuarioId();

        if (string.IsNullOrWhiteSpace(aprobadorId))
        {
            return Unauthorized();
        }

        var resultado = await _solicitudAccesoService.AprobarAsync(
            model.SolicitudId,
            aprobadorId,
            model.DuracionAprobadaMin,
            model.Observacion
        );

        if (!resultado.Success || string.IsNullOrWhiteSpace(resultado.TokenPlano) || string.IsNullOrWhiteSpace(resultado.CredencialId))
        {
            TempData["Error"] = resultado.Message;
            return RedirectToAction(nameof(Details), new { id = model.SolicitudId });
        }

        var tokenPlano = resultado.TokenPlano!;
        var credencialId = resultado.CredencialId!;

        var credencial = await _credencialTemporalService.GetByIdAsync(credencialId);
        var solicitud = await _solicitudAccesoService.GetByIdAsync(model.SolicitudId);
        var recurso = solicitud is null
            ? null
            : await _recursoService.GetByIdAsync(solicitud.RecursoId);
        
        string? urlRecursoExterno = null;
        if (
            recurso is not null &&
            !string.IsNullOrWhiteSpace(recurso.UrlExterna)
        )
        {
            Response.Cookies.Append(
                ExternalTokenCookieName,
                tokenPlano,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false,
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddMinutes(5)
                }
            );

            urlRecursoExterno = recurso.UrlExterna;
        }

        var viewModel = new ResultadoAprobacionViewModel
        {
            SolicitudId = model.SolicitudId,
            CredencialId = credencialId,
            TokenPlano = tokenPlano,
            RecursoNombre = recurso?.Nombre ?? "Recurso no disponible",
            ExpiresAt = credencial?.ExpiresAt ?? DateTime.UtcNow,
            MaxUsos = credencial?.MaxUsos ?? 0,
            UrlRecursoExterno = urlRecursoExterno
        };

        return View("Resultado", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rechazar(RechazarSolicitudViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "La observación es obligatoria para rechazar la solicitud.";
            return RedirectToAction(nameof(Details), new { id = model.SolicitudId });
        }

        var aprobadorId = ObtenerUsuarioId();

        if (string.IsNullOrWhiteSpace(aprobadorId))
        {
            return Unauthorized();
        }

        var resultado = await _solicitudAccesoService.RechazarAsync(
            model.SolicitudId,
            aprobadorId,
            model.Observacion
        );

        if (!resultado.Success)
        {
            TempData["Error"] = resultado.Message;
            return RedirectToAction(nameof(Details), new { id = model.SolicitudId });
        }

        TempData["Success"] = "Solicitud rechazada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private string? ObtenerUsuarioId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}