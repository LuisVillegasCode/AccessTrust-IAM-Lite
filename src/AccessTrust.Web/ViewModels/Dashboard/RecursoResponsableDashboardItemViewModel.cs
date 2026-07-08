namespace AccessTrust.Web.ViewModels.Dashboard;

public class RecursoResponsableDashboardItemViewModel
{
    public string Id { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public string Sensibilidad { get; set; } = string.Empty;

    public bool Activo { get; set; }

    public DateTime CreatedAt { get; set; }
}