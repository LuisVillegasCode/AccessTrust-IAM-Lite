namespace AccessTrust.Web.Services.Credentials;

public class RevocarCredencialResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? CredencialId { get; set; }

    public string? UsuarioId { get; set; }

    public string? RecursoId { get; set; }

    public DateTime? RevokedAt { get; set; }

    public static RevocarCredencialResult Ok(
        string credencialId,
        string usuarioId,
        string recursoId,
        DateTime revokedAt)
    {
        return new RevocarCredencialResult
        {
            Success = true,
            Message = "Credencial revocada correctamente.",
            CredencialId = credencialId,
            UsuarioId = usuarioId,
            RecursoId = recursoId,
            RevokedAt = revokedAt
        };
    }

    public static RevocarCredencialResult Fail(string message)
    {
        return new RevocarCredencialResult
        {
            Success = false,
            Message = message
        };
    }
}