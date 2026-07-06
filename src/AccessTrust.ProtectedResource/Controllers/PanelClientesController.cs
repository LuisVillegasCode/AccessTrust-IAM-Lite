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
    private const string ExternalTicketCookieName = "AccessTrust.ExternalTicket";
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
        var ticketPlano = Request.Cookies[ExternalTicketCookieName];

        if (string.IsNullOrWhiteSpace(ticketPlano))
        {
            var modelSinTicket = new PanelClientesResultadoViewModel
            {
                Permitido = false,
                Mensaje = "No se encontró el ticket externo enviado por el IAM.",
                MotivoCodigo = "TICKET_EXTERNO_NO_ENCONTRADO",
                RecursoId = RecursoId,
                RecursoNombre = RecursoNombre,
                RecursoTipo = RecursoTipo,
                RecursoSensibilidad = RecursoSensibilidad
            };

            return View("Resultado", modelSinTicket);
        }

        var resultado = await _iamValidationClient.ValidarTicketExternoAsync(
            RecursoId,
            ticketPlano
        );

        var model = new PanelClientesResultadoViewModel
        {
            Permitido = resultado.Permitido,
            Mensaje = resultado.Mensaje,
            MotivoCodigo = resultado.MotivoCodigo,
            RecursoId = resultado.RecursoId,
            RecursoNombre = string.IsNullOrWhiteSpace(resultado.RecursoNombre)
                ? RecursoNombre
                : resultado.RecursoNombre,
            RecursoTipo = string.IsNullOrWhiteSpace(resultado.RecursoTipo)
                ? RecursoTipo
                : resultado.RecursoTipo,
            RecursoSensibilidad = string.IsNullOrWhiteSpace(resultado.RecursoSensibilidad)
                ? RecursoSensibilidad
                : resultado.RecursoSensibilidad,
            CredencialId = resultado.CredencialId,
            ExpiresAt = resultado.ExpiresAt,
            UsosRealizados = resultado.UsosRealizados,
            MaxUsos = resultado.MaxUsos
        };

        return View("Resultado", model);
    }
}