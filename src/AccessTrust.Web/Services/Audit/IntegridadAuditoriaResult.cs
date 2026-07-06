namespace AccessTrust.Web.Services.Audit;

public class IntegridadAuditoriaResult
{
    public bool CadenaIntegra { get; set; }

    public int TotalEventosVerificados { get; set; }

    public int TotalEventosInvalidos { get; set; }

    public DateTime VerificadoAt { get; set; } = DateTime.UtcNow;

    public List<IntegridadEventoResult> Eventos { get; set; } = new();

    public string MensajeGeneral =>
        CadenaIntegra
            ? "La cadena de auditoría se encuentra íntegra."
            : "Se detectaron inconsistencias en la cadena de auditoría.";
}