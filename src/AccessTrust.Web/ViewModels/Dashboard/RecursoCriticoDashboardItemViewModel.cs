namespace AccessTrust.Web.ViewModels.Dashboard;

public class RecursoCriticoDashboardItemViewModel
{
    public string Id { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    public string Sensibilidad { get; set; } = string.Empty;

    public string ResponsableId { get; set; } = string.Empty;

    public string ResponsableNombre { get; set; } = string.Empty;

    public string ResponsableCorreo { get; set; } = string.Empty;

    public bool Activo { get; set; }
}