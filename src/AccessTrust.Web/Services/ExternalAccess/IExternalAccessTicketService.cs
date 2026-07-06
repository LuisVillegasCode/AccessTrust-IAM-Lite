namespace AccessTrust.Web.Services.ExternalAccess;

public interface IExternalAccessTicketService
{
    Task<CrearTicketAccesoExternoResult> CrearAsync(
        string usuarioId,
        string credencialId,
        string? createdByIp,
        string? userAgent);

    Task<ValidarTicketAccesoExternoResult> ValidarAsync(
        string recursoId,
        string ticketPlano);
}