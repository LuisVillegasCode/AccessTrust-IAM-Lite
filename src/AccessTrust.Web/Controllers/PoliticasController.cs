using AccessTrust.Web.Models;
using AccessTrust.Web.Services.Policies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessTrust.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class PoliticasController : Controller
{
    private readonly IPoliticaAccesoService _politicaAccesoService;

    public PoliticasController(IPoliticaAccesoService politicaAccesoService)
    {
        _politicaAccesoService = politicaAccesoService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var politicas = await _politicaAccesoService.GetAllAsync();
        return View(politicas);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var politica = await _politicaAccesoService.GetByIdAsync(id);

        if (politica is null)
        {
            return NotFound();
        }

        return View(politica);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, PoliticaAcceso politica)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        ValidarPolitica(politica);

        if (!ModelState.IsValid)
        {
            return View(politica);
        }

        var actualizado = await _politicaAccesoService.UpdateAsync(id, politica);

        if (!actualizado)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    private void ValidarPolitica(PoliticaAcceso politica)
    {
        if (string.IsNullOrWhiteSpace(politica.Nombre))
        {
            ModelState.AddModelError(nameof(politica.Nombre), "El nombre de la política es obligatorio.");
        }

        if (politica.DuracionMaxMin <= 0)
        {
            ModelState.AddModelError(nameof(politica.DuracionMaxMin), "La duración máxima debe ser mayor que cero.");
        }

        if (politica.MaxUsos <= 0)
        {
            ModelState.AddModelError(nameof(politica.MaxUsos), "El número máximo de usos debe ser mayor que cero.");
        }

        if (politica.IntentosOtpMax <= 0)
        {
            ModelState.AddModelError(nameof(politica.IntentosOtpMax), "Los intentos máximos de OTP deben ser mayores que cero.");
        }
    }
}