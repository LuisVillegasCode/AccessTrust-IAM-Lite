using System.Security.Claims;
using AccessTrust.Web.Models;
using AccessTrust.Web.Services.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessTrust.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class RecursosController : Controller
{
    private readonly IRecursoService _recursoService;

    public RecursosController(IRecursoService recursoService)
    {
        _recursoService = recursoService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var recursos = await _recursoService.GetAllAsync();
        return View(recursos);
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var recurso = await _recursoService.GetByIdAsync(id);

        if (recurso is null)
        {
            return NotFound();
        }

        return View(recurso);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var recurso = new Recurso
        {
            Activo = true
        };

        return View(recurso);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Recurso recurso)
    {
        ValidarRecurso(recurso);

        if (!ModelState.IsValid)
        {
            return View(recurso);
        }

        recurso.ResponsableId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        await _recursoService.CreateAsync(recurso);

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var recurso = await _recursoService.GetByIdAsync(id);

        if (recurso is null)
        {
            return NotFound();
        }

        return View(recurso);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, Recurso recurso)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        ValidarRecurso(recurso);

        if (!ModelState.IsValid)
        {
            return View(recurso);
        }

        var actualizado = await _recursoService.UpdateAsync(id, recurso);

        if (!actualizado)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    private void ValidarRecurso(Recurso recurso)
    {
        if (string.IsNullOrWhiteSpace(recurso.Nombre))
        {
            ModelState.AddModelError(nameof(recurso.Nombre), "El nombre del recurso es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(recurso.Tipo))
        {
            ModelState.AddModelError(nameof(recurso.Tipo), "El tipo del recurso es obligatorio.");
        }
    }
}