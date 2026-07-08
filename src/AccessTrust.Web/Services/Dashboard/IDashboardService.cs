using AccessTrust.Web.ViewModels.Dashboard;

namespace AccessTrust.Web.Services.Dashboard;

public interface IDashboardService
{
    Task<SolicitanteDashboardViewModel> GetSolicitanteDashboardAsync(
        string usuarioId,
        string usuarioNombre,
        string usuarioCorreo
    );

    Task<AprobadorDashboardViewModel> GetAprobadorDashboardAsync(
        string aprobadorId,
        string usuarioNombre,
        string usuarioCorreo
    );

    Task<AdministradorDashboardViewModel> GetAdministradorDashboardAsync(
        string usuarioNombre,
        string usuarioCorreo
    );
}