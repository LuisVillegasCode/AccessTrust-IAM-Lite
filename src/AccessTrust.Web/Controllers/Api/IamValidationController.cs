using AccessTrust.Web.ViewModels.Api;
using AccessTrust.Web.Services.Credentials;
using AccessTrust.Web.Services.Resources;
using Microsoft.AspNetCore.Mvc;

namespace AccessTrust.Web.Controllers.Api;

[ApiController]
[Route("api/iam")]
public class IamValidationController : ControllerBase
{
    private readonly IRecursoService _recursoService;
    private readonly ICredencialTemporalService _credencialTemporalService;

    public IamValidationController(
        IRecursoService recursoService,
        ICredencialTemporalService credencialTemporalService)
    {
        _recursoService = recursoService;
        _credencialTemporalService = credencialTemporalService;
    }

    [HttpPost("validar-credencial")]
    public async Task<ActionResult<ValidarCredencialApiResponse>> ValidarCredencial(
        [FromBody] ValidarCredencialApiRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RecursoId))
        {
            return BadRequest(new ValidarCredencialApiResponse
            {
                Permitido = false,
                Mensaje = "El recurso es obligatorio."
            });
        }

        if (string.IsNullOrWhiteSpace(request.TokenPlano))
        {
            return BadRequest(new ValidarCredencialApiResponse
            {
                Permitido = false,
                Mensaje = "El token temporal es obligatorio."
            });
        }

        var recurso = await _recursoService.GetByIdAsync(request.RecursoId);

        if (recurso is null)
        {
            return NotFound(new ValidarCredencialApiResponse
            {
                Permitido = false,
                Mensaje = "El recurso solicitado no existe.",
                RecursoId = request.RecursoId
            });
        }

        var resultado = await _credencialTemporalService.ValidarTokenAsync(
            request.RecursoId,
            request.TokenPlano
        );

        var response = new ValidarCredencialApiResponse
        {
            Permitido = resultado.Permitido,
            Mensaje = resultado.Mensaje,
            RecursoId = request.RecursoId,
            RecursoNombre = recurso.Nombre,
            RecursoTipo = recurso.Tipo,
            RecursoSensibilidad = recurso.Sensibilidad.ToString(),
            CredencialId = resultado.Credencial?.Id,
            ExpiresAt = resultado.Credencial?.ExpiresAt,
            UsosRealizados = resultado.Credencial?.UsosRealizados,
            MaxUsos = resultado.Credencial?.MaxUsos
        };

        return Ok(response);
    }
}