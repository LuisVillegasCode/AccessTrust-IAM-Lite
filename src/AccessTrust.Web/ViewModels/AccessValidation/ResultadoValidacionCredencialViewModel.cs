namespace AccessTrust.Web.ViewModels.AccessValidation;

public class ResultadoValidacionCredencialViewModel
{
    public bool Permitido { get; set; }

    public string Mensaje { get; set; } = string.Empty;

    public string RecursoId { get; set; } = string.Empty;

    public string RecursoNombre { get; set; } = string.Empty;

    public string RecursoTipo { get; set; } = string.Empty;

    public string RecursoSensibilidad { get; set; } = string.Empty;

    public string? CredencialId { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public int? UsosRealizados { get; set; }

    public int? MaxUsos { get; set; }
}