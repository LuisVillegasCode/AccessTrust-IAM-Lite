namespace AccessTrust.ProtectedResource.Dtos;

public class ValidarTicketExternoRequest
{
    public string RecursoId { get; set; } = string.Empty;

    public string TicketPlano { get; set; } = string.Empty;
}