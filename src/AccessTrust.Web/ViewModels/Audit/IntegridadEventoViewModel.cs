namespace AccessTrust.Web.ViewModels.Audit;

public class IntegridadEventoViewModel
{
    public string EventoId { get; set; } = string.Empty;

    public long Seq { get; set; }

    public string Accion { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public bool PrevHashValido { get; set; }

    public bool HashValido { get; set; }

    public bool EsValido => PrevHashValido && HashValido;

    public string Mensaje { get; set; } = string.Empty;
}