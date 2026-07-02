using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessTrust.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
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