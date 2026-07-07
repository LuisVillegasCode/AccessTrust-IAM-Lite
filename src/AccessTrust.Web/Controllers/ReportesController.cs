using AccessTrust.Web.Services.Reports;
using AccessTrust.Web.ViewModels.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessTrust.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class ReportesController : Controller
{
    private const int PaginaDefault = 1;
    private const int TamanoPaginaDefault = 10;

    private readonly IReporteService _reporteService;

    public ReportesController(IReporteService reporteService)
    {
        _reporteService = reporteService;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ReportesFiltroViewModel filtro)
    {
        filtro ??= new ReportesFiltroViewModel();

        var dashboard = await _reporteService.GenerarDashboardAsync(filtro);

        return View(dashboard);
    }

    [HttpGet]
    public async Task<IActionResult> BuscarUsuarios(string term)
    {
        var usuarios = await _reporteService.BuscarUsuariosAsync(term);

        return Json(usuarios);
    }

    [HttpGet]
    public async Task<IActionResult> SolicitudesDetalle(
        [FromQuery] ReportesFiltroViewModel filtro,
        [FromQuery] int pagina = PaginaDefault,
        [FromQuery] int tamanoPagina = TamanoPaginaDefault)
    {
        filtro ??= new ReportesFiltroViewModel();

        var detalle = await _reporteService.ListarSolicitudesDetalleAsync(
            filtro,
            pagina,
            tamanoPagina
        );

        return View(detalle);
    }

    [HttpGet]
    public async Task<IActionResult> CredencialesDetalle(
        [FromQuery] ReportesFiltroViewModel filtro,
        [FromQuery] int pagina = PaginaDefault,
        [FromQuery] int tamanoPagina = TamanoPaginaDefault)
    {
        filtro ??= new ReportesFiltroViewModel();

        var detalle = await _reporteService.ListarCredencialesDetalleAsync(
            filtro,
            pagina,
            tamanoPagina
        );

        return View(detalle);
    }

    [HttpGet]
    public async Task<IActionResult> TicketsDetalle(
        [FromQuery] ReportesFiltroViewModel filtro,
        [FromQuery] int pagina = PaginaDefault,
        [FromQuery] int tamanoPagina = TamanoPaginaDefault)
    {
        filtro ??= new ReportesFiltroViewModel();

        var detalle = await _reporteService.ListarTicketsDetalleAsync(
            filtro,
            pagina,
            tamanoPagina
        );

        return View(detalle);
    }

    [HttpGet]
    public async Task<IActionResult> AuditoriaDetalle(
        [FromQuery] ReportesFiltroViewModel filtro,
        [FromQuery] int pagina = PaginaDefault,
        [FromQuery] int tamanoPagina = TamanoPaginaDefault)
    {
        filtro ??= new ReportesFiltroViewModel();

        var detalle = await _reporteService.ListarAuditoriaDetalleAsync(
            filtro,
            pagina,
            tamanoPagina
        );

        return View(detalle);
    }

    [HttpGet]
    public async Task<IActionResult> RecursosDetalle(
        [FromQuery] ReportesFiltroViewModel filtro,
        [FromQuery] int pagina = PaginaDefault,
        [FromQuery] int tamanoPagina = TamanoPaginaDefault)
    {
        filtro ??= new ReportesFiltroViewModel();

        var detalle = await _reporteService.ListarRecursosDetalleAsync(
            filtro,
            pagina,
            tamanoPagina
        );

        return View(detalle);
    }
}