namespace AccessTrust.Web.Services.ExternalAccess;

public class CrearTicketAccesoExternoResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? TicketPlano { get; set; }

    public string? TicketId { get; set; }

    public string? CredencialId { get; set; }

    public string? RecursoId { get; set; }

    public string? UrlExterna { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public static CrearTicketAccesoExternoResult Ok(
        string ticketPlano,
        string ticketId,
        string credencialId,
        string recursoId,
        string urlExterna,
        DateTime expiresAt)
    {
        return new CrearTicketAccesoExternoResult
        {
            Success = true,
            Message = "Ticket externo generado correctamente.",
            TicketPlano = ticketPlano,
            TicketId = ticketId,
            CredencialId = credencialId,
            RecursoId = recursoId,
            UrlExterna = urlExterna,
            ExpiresAt = expiresAt
        };
    }

    public static CrearTicketAccesoExternoResult Fail(string message)
    {
        return new CrearTicketAccesoExternoResult
        {
            Success = false,
            Message = message
        };
    }
}