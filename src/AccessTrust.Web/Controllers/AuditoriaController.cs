using AccessTrust.Web.Services.Audit;
using AccessTrust.Web.ViewModels.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessTrust.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class AuditoriaController : Controller
{
    private readonly IAuditService _auditService;

    public AuditoriaController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(AuditoriaIndexViewModel filtroViewModel)
    {
        var filtro = new AuditoriaFiltro
        {
            Accion = filtroViewModel.Accion,
            Resultado = filtroViewModel.Resultado,
            ActorUserId = filtroViewModel.ActorUserId,
            EntidadTipo = filtroViewModel.EntidadTipo,
            FechaDesde = filtroViewModel.FechaDesde,
            FechaHasta = filtroViewModel.FechaHasta,
            Limite = 100
        };

        var eventos = await _auditService.ListarEventosAsync(filtro);

        filtroViewModel.Eventos = eventos.Select(e => new EventoAuditoriaListItemViewModel
        {
            Id = e.Id ?? string.Empty,
            Seq = e.Seq,
            ActorUserId = e.ActorUserId,
            Accion = e.Accion,
            EntidadTipo = e.EntidadTipo,
            EntidadId = e.EntidadId,
            Resultado = e.Resultado,
            CreatedAt = e.CreatedAt,
            Hash = e.Hash
        }).ToList();

        return View(filtroViewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        var evento = await _auditService.GetByIdAsync(id);

        if (evento is null)
        {
            TempData["Error"] = "El evento de auditoría no existe.";
            return RedirectToAction(nameof(Index));
        }

        var viewModel = new EventoAuditoriaDetailsViewModel
        {
            Id = evento.Id ?? string.Empty,
            Seq = evento.Seq,
            ActorUserId = evento.ActorUserId,
            Accion = evento.Accion,
            EntidadTipo = evento.EntidadTipo,
            EntidadId = evento.EntidadId,
            Resultado = evento.Resultado,
            Detalle = evento.Detalle,
            PrevHash = evento.PrevHash,
            Hash = evento.Hash,
            CreatedAt = evento.CreatedAt
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> VerificarIntegridad()
    {
        var resultado = await _auditService.VerificarIntegridadAsync();

        var viewModel = new IntegridadAuditoriaResultadoViewModel
        {
            CadenaIntegra = resultado.CadenaIntegra,
            TotalEventosVerificados = resultado.TotalEventosVerificados,
            TotalEventosInvalidos = resultado.TotalEventosInvalidos,
            VerificadoAt = resultado.VerificadoAt,
            Eventos = resultado.Eventos.Select(e => new IntegridadEventoViewModel
            {
                EventoId = e.EventoId,
                Seq = e.Seq,
                Accion = e.Accion,
                CreatedAt = e.CreatedAt,
                PrevHashValido = e.PrevHashValido,
                HashValido = e.HashValido,
                Mensaje = e.Mensaje
            }).ToList()
        };

        return View(viewModel);
    }
}