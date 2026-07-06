using System.Security.Claims;
using AccessTrust.Web.Models;
using AccessTrust.Web.Services.Credentials;
using AccessTrust.Web.Services.ExternalAccess;
using AccessTrust.Web.Services.Resources;
using AccessTrust.Web.ViewModels.AccessValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessTrust.Web.Controllers;

[Authorize(Roles = "Solicitante")]
public class MisAccesosController : Controller
{
    private const string ExternalTicketCookieName = "AccessTrust.ExternalTicket";

    private readonly ICredencialTemporalService _credencialTemporalService;
    private readonly IRecursoService _recursoService;
    private readonly IExternalAccessTicketService _externalAccessTicketService;

    public MisAccesosController(
        ICredencialTemporalService credencialTemporalService,
        IRecursoService recursoService,
        IExternalAccessTicketService externalAccessTicketService)
    {
        _credencialTemporalService = credencialTemporalService;
        _recursoService = recursoService;
        _externalAccessTicketService = externalAccessTicketService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var usuarioId = ObtenerUsuarioId();

        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return Unauthorized();
        }

        var credenciales = await _credencialTemporalService.GetByUsuarioAsync(usuarioId);
        var viewModels = new List<MisAccesoActivoViewModel>();

        foreach (var credencial in credenciales)
        {
            if (
                credencial.Estado != EstadoCredencial.Activa ||
                credencial.ExpiresAt <= DateTime.UtcNow ||
                credencial.UsosRealizados >= credencial.MaxUsos
            )
            {
                continue;
            }

            var recurso = await _recursoService.GetByIdAsync(credencial.RecursoId);

            if (recurso is null || !recurso.Activo)
            {
                continue;
            }

            viewModels.Add(new MisAccesoActivoViewModel
            {
                CredencialId = credencial.Id ?? string.Empty,
                RecursoId = credencial.RecursoId,
                RecursoNombre = recurso.Nombre,
                RecursoTipo = recurso.Tipo,
                RecursoSensibilidad = recurso.Sensibilidad,
                Estado = credencial.Estado,
                ExpiresAt = credencial.ExpiresAt,
                UsosRealizados = credencial.UsosRealizados,
                MaxUsos = credencial.MaxUsos,
                TieneUrlExterna = !string.IsNullOrWhiteSpace(recurso.UrlExterna)
            });
        }

        return View(viewModels);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AbrirExterno(string credencialId)
    {
        var usuarioId = ObtenerUsuarioId();

        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(credencialId))
        {
            TempData["Error"] = "La credencial temporal no es válida.";
            return RedirectToAction(nameof(Index));
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var resultado = await _externalAccessTicketService.CrearAsync(
            usuarioId,
            credencialId,
            ip,
            userAgent
        );

        if (
            !resultado.Success ||
            string.IsNullOrWhiteSpace(resultado.TicketPlano) ||
            string.IsNullOrWhiteSpace(resultado.UrlExterna) ||
            resultado.ExpiresAt is null
        )
        {
            TempData["Error"] = resultado.Message;
            return RedirectToAction(nameof(Index));
        }

        Response.Cookies.Append(
            ExternalTicketCookieName,
            resultado.TicketPlano,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = resultado.ExpiresAt.Value
            }
        );

        return Redirect(resultado.UrlExterna);
    }

    private string? ObtenerUsuarioId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}