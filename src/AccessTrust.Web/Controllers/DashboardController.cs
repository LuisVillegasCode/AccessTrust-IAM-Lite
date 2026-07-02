using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessTrust.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    [Authorize(Roles = "Solicitante")]
    public IActionResult Solicitante()
    {
        return Content("Dashboard del Solicitante: acceso autorizado.");
    }

    [Authorize(Roles = "Aprobador")]
    public IActionResult Aprobador()
    {
        return Content("Dashboard del Aprobador: acceso autorizado.");
    }

    [Authorize(Roles = "Administrador")]
    public IActionResult Administrador()
    {
        return Content("Dashboard del Administrador: acceso autorizado.");
    }
}