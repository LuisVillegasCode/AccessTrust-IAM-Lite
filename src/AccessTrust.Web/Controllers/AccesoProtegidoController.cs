using AccessTrust.Web.Services.Credentials;
using AccessTrust.Web.Services.Resources;
using AccessTrust.Web.ViewModels.AccessValidation;
using Microsoft.AspNetCore.Mvc;

namespace AccessTrust.Web.Controllers;

public class AccesoProtegidoController : Controller
{
    private readonly IRecursoService _recursoService;
    private readonly ICredencialTemporalService _credencialTemporalService;

    public AccesoProtegidoController(
        IRecursoService recursoService,
        ICredencialTemporalService credencialTemporalService)
    {
        _recursoService = recursoService;
        _credencialTemporalService = credencialTemporalService;
    }

    public async Task<IActionResult> Index()
    {
        var recursos = await _recursoService.GetActivosAsync();

        var viewModels = recursos.Select(r => new RecursoProtegidoViewModel
        {
            RecursoId = r.Id ?? string.Empty,
            RecursoNombre = r.Nombre,
            RecursoTipo = r.Tipo,
            RecursoSensibilidad = r.Sensibilidad.ToString(),
            ContenidoSimulado = string.Empty
        }).ToList();

        return View(viewModels);
    }

    public async Task<IActionResult> Validar(string recursoId)
    {
        var recurso = await _recursoService.GetByIdAsync(recursoId);

        if (recurso is null)
        {
            return NotFound();
        }

        var viewModel = new ValidarCredencialViewModel
        {
            RecursoId = recurso.Id ?? string.Empty,
            RecursoNombre = recurso.Nombre,
            RecursoTipo = recurso.Tipo,
            RecursoSensibilidad = recurso.Sensibilidad.ToString()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Validar(ValidarCredencialViewModel model)
    {
        var recurso = await _recursoService.GetByIdAsync(model.RecursoId);

        if (recurso is null)
        {
            return NotFound();
        }

        model.RecursoNombre = recurso.Nombre;
        model.RecursoTipo = recurso.Tipo;
        model.RecursoSensibilidad = recurso.Sensibilidad.ToString();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var resultado = await _credencialTemporalService.ValidarTokenAsync(
            model.RecursoId,
            model.TokenPlano
        );

        if (!resultado.Permitido)
        {
            var denegadoViewModel = new ResultadoValidacionCredencialViewModel
            {
                Permitido = false,
                Mensaje = resultado.Mensaje,
                RecursoId = model.RecursoId,
                RecursoNombre = recurso.Nombre,
                RecursoTipo = recurso.Tipo,
                RecursoSensibilidad = recurso.Sensibilidad.ToString(),
                CredencialId = resultado.Credencial?.Id,
                ExpiresAt = resultado.Credencial?.ExpiresAt,
                UsosRealizados = resultado.Credencial?.UsosRealizados,
                MaxUsos = resultado.Credencial?.MaxUsos
            };

            return View("Resultado", denegadoViewModel);
        }

        var permitidoViewModel = new ResultadoValidacionCredencialViewModel
        {
            Permitido = true,
            Mensaje = resultado.Mensaje,
            RecursoId = model.RecursoId,
            RecursoNombre = recurso.Nombre,
            RecursoTipo = recurso.Tipo,
            RecursoSensibilidad = recurso.Sensibilidad.ToString(),
            CredencialId = resultado.Credencial?.Id,
            ExpiresAt = resultado.Credencial?.ExpiresAt,
            UsosRealizados = resultado.Credencial?.UsosRealizados,
            MaxUsos = resultado.Credencial?.MaxUsos
        };

        return View("Resultado", permitidoViewModel);
    }
}