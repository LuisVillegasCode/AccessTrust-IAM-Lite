using System.Security.Claims;
using AccessTrust.Web.Services.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessTrust.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public IActionResult Index()
    {
        if (User.IsInRole("Administrador"))
        {
            return RedirectToAction(nameof(Administrador));
        }

        if (User.IsInRole("Aprobador"))
        {
            return RedirectToAction(nameof(Aprobador));
        }

        if (User.IsInRole("Solicitante"))
        {
            return RedirectToAction(nameof(Solicitante));
        }

        return RedirectToAction("AccessDenied", "Account");
    }

    [Authorize(Roles = "Solicitante")]
    public async Task<IActionResult> Solicitante()
    {
        var usuario = GetUsuarioActual();

        if (string.IsNullOrWhiteSpace(usuario.UsuarioId))
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var model = await _dashboardService.GetSolicitanteDashboardAsync(
            usuario.UsuarioId,
            usuario.UsuarioNombre,
            usuario.UsuarioCorreo
        );

        return View(model);
    }

    [Authorize(Roles = "Aprobador")]
    public async Task<IActionResult> Aprobador()
    {
        var usuario = GetUsuarioActual();

        if (string.IsNullOrWhiteSpace(usuario.UsuarioId))
        {
            return RedirectToAction("AccessDenied", "Account");
        }

        var model = await _dashboardService.GetAprobadorDashboardAsync(
            usuario.UsuarioId,
            usuario.UsuarioNombre,
            usuario.UsuarioCorreo
        );

        return View(model);
    }

    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Administrador()
    {
        var usuario = GetUsuarioActual();

        var model = await _dashboardService.GetAdministradorDashboardAsync(
            usuario.UsuarioNombre,
            usuario.UsuarioCorreo
        );

        return View(model);
    }

    private (string UsuarioId, string UsuarioNombre, string UsuarioCorreo) GetUsuarioActual()
    {
        var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var usuarioNombre = User.FindFirst(ClaimTypes.Name)?.Value ?? User.Identity?.Name ?? "Usuario";
        var usuarioCorreo = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

        return (usuarioId, usuarioNombre, usuarioCorreo);
    }
}