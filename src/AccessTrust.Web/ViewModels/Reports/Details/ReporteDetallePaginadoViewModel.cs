namespace AccessTrust.Web.ViewModels.Reports;

public class ReporteDetallePaginadoViewModel<TItem>
{
    public string Titulo { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public string SeccionOrigen { get; set; } = string.Empty;

    public ReportesFiltroViewModel Filtro { get; set; } = new();

    public List<TItem> Registros { get; set; } = new();

    public int PaginaActual { get; set; } = 1;

    public int TamanoPagina { get; set; } = 10;

    public long TotalRegistros { get; set; }

    public int TotalPaginas =>
        TamanoPagina <= 0
            ? 1
            : Math.Max(1, (int)Math.Ceiling(TotalRegistros / (double)TamanoPagina));

    public bool TienePaginaAnterior => PaginaActual > 1;

    public bool TienePaginaSiguiente => PaginaActual < TotalPaginas;
}