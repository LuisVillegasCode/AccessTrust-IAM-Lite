using AccessTrust.ProtectedResource.Dtos;

namespace AccessTrust.ProtectedResource.Services;

public interface IIamValidationClient
{
    Task<ValidarCredencialResponse> ValidarCredencialAsync(
        string recursoId,
        string tokenPlano);

    Task<ValidarTicketExternoResponse> ValidarTicketExternoAsync(
        string recursoId,
        string ticketPlano);
}