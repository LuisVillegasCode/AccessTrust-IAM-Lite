namespace AccessTrust.Web.ViewModels.Dashboard;

public class AlertaSeguridadDashboardItemViewModel
{
    public string Id { get; set; } = string.Empty;

    public string Severidad { get; set; } = string.Empty;

    public string Mensaje { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}