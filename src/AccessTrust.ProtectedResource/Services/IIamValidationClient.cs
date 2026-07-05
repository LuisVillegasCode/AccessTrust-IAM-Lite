using AccessTrust.ProtectedResource.Dtos;

namespace AccessTrust.ProtectedResource.Services;

public interface IIamValidationClient
{
    Task<ValidarCredencialResponse> ValidarCredencialAsync(
        string recursoId,
        string tokenPlano);
}