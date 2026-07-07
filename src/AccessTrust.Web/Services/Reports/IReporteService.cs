using AccessTrust.Web.ViewModels.Reports;

namespace AccessTrust.Web.Services.Reports;

public interface IReporteService
{
    Task<ReportesDashboardViewModel> GenerarDashboardAsync(
        ReportesFiltroViewModel filtro
    );
    Task<List<ReporteFiltroOpcionViewModel>> BuscarUsuariosAsync(
        string term,
        int limite = 10
    );
}