using AccessTrust.Web.Services.Reports;
using AccessTrust.Web.ViewModels.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessTrust.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class ReportesController : Controller
{
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
}