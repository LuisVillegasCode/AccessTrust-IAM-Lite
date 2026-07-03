namespace AccessTrust.Web.ViewModels.Approvals;

public class ResultadoAprobacionViewModel
{
    public string SolicitudId { get; set; } = string.Empty;

    public string CredencialId { get; set; } = string.Empty;

    public string TokenPlano { get; set; } = string.Empty;

    public string RecursoNombre { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public int MaxUsos { get; set; }
}