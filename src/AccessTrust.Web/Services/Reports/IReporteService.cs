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

    Task<ReporteDetallePaginadoViewModel<SolicitudDetalleReporteViewModel>> ListarSolicitudesDetalleAsync(
        ReportesFiltroViewModel filtro,
        int pagina,
        int tamanoPagina
    );

    Task<ReporteDetallePaginadoViewModel<CredencialDetalleReporteViewModel>> ListarCredencialesDetalleAsync(
        ReportesFiltroViewModel filtro,
        int pagina,
        int tamanoPagina
    );

    Task<ReporteDetallePaginadoViewModel<TicketDetalleReporteViewModel>> ListarTicketsDetalleAsync(
        ReportesFiltroViewModel filtro,
        int pagina,
        int tamanoPagina
    );

    Task<ReporteDetallePaginadoViewModel<EventoAuditoriaDetalleReporteViewModel>> ListarAuditoriaDetalleAsync(
        ReportesFiltroViewModel filtro,
        int pagina,
        int tamanoPagina
    );

    Task<ReporteDetallePaginadoViewModel<RecursoDetalleReporteViewModel>> ListarRecursosDetalleAsync(
        ReportesFiltroViewModel filtro,
        int pagina,
        int tamanoPagina
    );
}