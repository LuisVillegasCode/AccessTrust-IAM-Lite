namespace AccessTrust.ProtectedResource.Dtos;

public class ValidarCredencialRequest
{
    public string RecursoId { get; set; } = string.Empty;

    public string TokenPlano { get; set; } = string.Empty;
}