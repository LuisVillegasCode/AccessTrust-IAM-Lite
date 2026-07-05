using AccessTrust.ProtectedResource.Services;
using AccessTrust.ProtectedResource.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AccessTrust.ProtectedResource.Controllers;

public class PanelClientesController : Controller
{
    private const string RecursoId = "6a4549b9442c51f54a2b0af6";
    private const string RecursoNombre = "Panel restringido de clientes";
    private const string RecursoTipo = "Panel";
    private const string RecursoSensibilidad = "Alta";
    private const string ExternalTokenCookieName = "AccessTrust.ExternalToken";

    private readonly IIamValidationClient _iamValidationClient;

    public PanelClientesController(IIamValidationClient iamValidationClient)
    {
        _iamValidationClient = iamValidationClient;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var model = new PanelClientesAccessViewModel
        {
            RecursoId = RecursoId,
            RecursoNombre = RecursoNombre,
            RecursoTipo = RecursoTipo,
            RecursoSensibilidad = RecursoSensibilidad
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Auto()
    {
        var tokenPlano = Request.Cookies[ExternalTokenCookieName];

        if (string.IsNullOrWhiteSpace(tokenPlano))
        {
            return Content(
                "Acceso denegado: no se encontró el token temporal enviado por el IAM."
            );
        }

        var resultado = await _iamValidationClient.ValidarCredencialAsync(
            RecursoId,
            tokenPlano
        );

        if (!resultado.Permitido)
        {
            return Content($"Acceso denegado: {resultado.Mensaje}");
        }

        return Content(
            $"Acceso concedido al {resultado.RecursoNombre}. " +
            $"Usos: {resultado.UsosRealizados}/{resultado.MaxUsos}. " +
            $"Expira: {resultado.ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC."
        );
    }
}