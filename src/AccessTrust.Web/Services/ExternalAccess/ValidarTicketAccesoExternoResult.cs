namespace AccessTrust.Web.Services.ExternalAccess;

public class ValidarTicketAccesoExternoResult
{
    public bool Permitido { get; set; }

    public string Mensaje { get; set; } = string.Empty;

    public string MotivoCodigo { get; set; } = string.Empty;

    public string RecursoId { get; set; } = string.Empty;

    public string RecursoNombre { get; set; } = string.Empty;

    public string RecursoTipo { get; set; } = string.Empty;

    public string RecursoSensibilidad { get; set; } = string.Empty;

    public string? CredencialId { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public int? UsosRealizados { get; set; }

    public int? MaxUsos { get; set; }

    public static ValidarTicketAccesoExternoResult Ok(
        string recursoId,
        string recursoNombre,
        string recursoTipo,
        string recursoSensibilidad,
        string credencialId,
        DateTime expiresAt,
        int usosRealizados,
        int maxUsos)
    {
        return new ValidarTicketAccesoExternoResult
        {
            Permitido = true,
            Mensaje = "Acceso permitido.",
            MotivoCodigo = "ACCESO_PERMITIDO",
            RecursoId = recursoId,
            RecursoNombre = recursoNombre,
            RecursoTipo = recursoTipo,
            RecursoSensibilidad = recursoSensibilidad,
            CredencialId = credencialId,
            ExpiresAt = expiresAt,
            UsosRealizados = usosRealizados,
            MaxUsos = maxUsos
        };
    }

    public static ValidarTicketAccesoExternoResult Fail(
        string mensaje,
        string motivoCodigo,
        string recursoId = "")
    {
        return new ValidarTicketAccesoExternoResult
        {
            Permitido = false,
            Mensaje = mensaje,
            MotivoCodigo = motivoCodigo,
            RecursoId = recursoId
        };
    }
}