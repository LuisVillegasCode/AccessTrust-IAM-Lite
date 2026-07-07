using AccessTrust.Web.ViewModels.Reports;

namespace AccessTrust.Web.Services.Reports;

public interface IReporteService
{
    Task<ReportesDashboardViewModel> GenerarDashboardAsync(
        ReportesFiltroViewModel filtro
    );
}