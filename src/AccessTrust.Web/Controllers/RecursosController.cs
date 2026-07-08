using System.Security.Claims;
using AccessTrust.Web.Models;
using AccessTrust.Web.Services.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

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

        await CargarAprobadoresAsync(recurso.ResponsableId);

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
            await CargarAprobadoresAsync(recurso.ResponsableId);
            return View(recurso);
        }

        var actorUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        var resultado = await _recursoService.UpdateAsync(
            id,
            recurso,
            actorUserId
        );

        if (!resultado.Success)
        {
            if (resultado.Message == "El recurso no existe.")
            {
                return NotFound();
            }

            ModelState.AddModelError(nameof(recurso.ResponsableId), resultado.Message);
            await CargarAprobadoresAsync(recurso.ResponsableId);
            return View(recurso);
        }

        TempData["Success"] = resultado.Message;

        return RedirectToAction(nameof(Index));
    }

    private async Task CargarAprobadoresAsync(string? responsableIdSeleccionado = null)
    {
        var aprobadores = await _recursoService.GetAprobadoresActivosAsync();

        var opciones = aprobadores.Select(u => new
        {
            u.Id,
            Texto = $"{u.Nombre} ({u.Correo})"
        });

        ViewBag.Aprobadores = new SelectList(
            opciones,
            "Id",
            "Texto",
            responsableIdSeleccionado
        );
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