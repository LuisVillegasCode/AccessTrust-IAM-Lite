using System.Net.Http.Json;
using AccessTrust.ProtectedResource.Dtos;

namespace AccessTrust.ProtectedResource.Services;

public class IamValidationClient : IIamValidationClient
{
    private readonly HttpClient _httpClient;

    public IamValidationClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ValidarCredencialResponse> ValidarCredencialAsync(
        string recursoId,
        string tokenPlano)
    {
        var request = new ValidarCredencialRequest
        {
            RecursoId = recursoId,
            TokenPlano = tokenPlano
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/iam/validar-credencial",
                request
            );

            var result = await response.Content
                .ReadFromJsonAsync<ValidarCredencialResponse>();

            if (result is null)
            {
                return new ValidarCredencialResponse
                {
                    Permitido = false,
                    Mensaje = "El IAM no devolvió una respuesta válida."
                };
            }

            return result;
        }
        catch (HttpRequestException)
        {
            return new ValidarCredencialResponse
            {
                Permitido = false,
                Mensaje = "No se pudo conectar con el IAM."
            };
        }
        catch (TaskCanceledException)
        {
            return new ValidarCredencialResponse
            {
                Permitido = false,
                Mensaje = "La consulta al IAM excedió el tiempo de espera."
            };
        }
    }
}