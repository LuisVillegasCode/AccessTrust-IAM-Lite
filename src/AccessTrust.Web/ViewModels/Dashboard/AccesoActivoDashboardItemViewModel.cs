namespace AccessTrust.Web.ViewModels.Dashboard;

public class AccesoActivoDashboardItemViewModel
{
    public string CredencialId { get; set; } = string.Empty;

    public string RecursoNombre { get; set; } = string.Empty;

    public string RecursoTipo { get; set; } = string.Empty;

    public string Sensibilidad { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public DateTime IssuedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public int UsosRealizados { get; set; }

    public int UsosMaximos { get; set; }

    public bool ProximoAExpirar { get; set; }
}