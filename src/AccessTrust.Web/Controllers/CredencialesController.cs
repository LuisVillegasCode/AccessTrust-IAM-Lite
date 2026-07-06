using System.Security.Claims;
using AccessTrust.Web.Models;
using AccessTrust.Web.Services.Credentials;
using AccessTrust.Web.Services.Resources;
using AccessTrust.Web.ViewModels.Credentials;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessTrust.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class CredencialesController : Controller
{
    private readonly ICredencialTemporalService _credencialTemporalService;
    private readonly IRecursoService _recursoService;

    public CredencialesController(
        ICredencialTemporalService credencialTemporalService,
        IRecursoService recursoService)
    {
        _credencialTemporalService = credencialTemporalService;
        _recursoService = recursoService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var credenciales = await _credencialTemporalService.GetActivasAsync();
        var viewModels = new List<CredencialActivaListItemViewModel>();

        foreach (var credencial in credenciales)
        {
            var recurso = await _recursoService.GetByIdAsync(credencial.RecursoId);

            viewModels.Add(new CredencialActivaListItemViewModel
            {
                CredencialId = credencial.Id ?? string.Empty,
                UsuarioId = credencial.UsuarioId,
                RecursoId = credencial.RecursoId,
                RecursoNombre = recurso?.Nombre ?? "Recurso no encontrado",
                RecursoTipo = recurso?.Tipo ?? "No disponible",
                RecursoSensibilidad = recurso?.Sensibilidad ?? SensibilidadRecurso.Baja,
                Estado = credencial.Estado,
                IssuedAt = credencial.IssuedAt,
                ExpiresAt = credencial.ExpiresAt,
                UsosRealizados = credencial.UsosRealizados,
                MaxUsos = credencial.MaxUsos,
                TieneUrlExterna = !string.IsNullOrWhiteSpace(recurso?.UrlExterna)
            });
        }

        return View(viewModels);
    }

    [HttpGet]
    public async Task<IActionResult> Revocar(string id)
    {
        var viewModel = await ConstruirRevocarViewModelAsync(id);

        if (viewModel is null)
        {
            TempData["Error"] = "La credencial temporal no existe o no puede revocarse.";
            return RedirectToAction(nameof(Index));
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Revocar(RevocarCredencialViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var viewModel = await ConstruirRevocarViewModelAsync(model.CredencialId);

            if (viewModel is null)
            {
                TempData["Error"] = "La credencial temporal no existe o no puede revocarse.";
                return RedirectToAction(nameof(Index));
            }

            viewModel.MotivoRevocacion = model.MotivoRevocacion;
            return View(viewModel);
        }

        var actorUserId = ObtenerUsuarioId();

        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return Unauthorized();
        }

        var resultado = await _credencialTemporalService.RevocarAsync(
            model.CredencialId,
            actorUserId,
            model.MotivoRevocacion
        );

        if (!resultado.Success)
        {
            TempData["Error"] = resultado.Message;
            return RedirectToAction(nameof(Revocar), new { id = model.CredencialId });
        }

        TempData["Success"] = "Credencial temporal revocada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<RevocarCredencialViewModel?> ConstruirRevocarViewModelAsync(string credencialId)
    {
        var credencial = await _credencialTemporalService.GetByIdAsync(credencialId);

        if (credencial is null)
        {
            return null;
        }

        if (
            credencial.Estado != EstadoCredencial.Activa ||
            credencial.ExpiresAt <= DateTime.UtcNow ||
            credencial.UsosRealizados >= credencial.MaxUsos
        )
        {
            return null;
        }

        var recurso = await _recursoService.GetByIdAsync(credencial.RecursoId);

        return new RevocarCredencialViewModel
        {
            CredencialId = credencial.Id ?? string.Empty,
            UsuarioId = credencial.UsuarioId,
            RecursoId = credencial.RecursoId,
            RecursoNombre = recurso?.Nombre ?? "Recurso no encontrado",
            RecursoTipo = recurso?.Tipo ?? "No disponible",
            RecursoSensibilidad = recurso?.Sensibilidad ?? SensibilidadRecurso.Baja,
            Estado = credencial.Estado,
            IssuedAt = credencial.IssuedAt,
            ExpiresAt = credencial.ExpiresAt,
            UsosRealizados = credencial.UsosRealizados,
            MaxUsos = credencial.MaxUsos
        };
    }

    private string? ObtenerUsuarioId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}