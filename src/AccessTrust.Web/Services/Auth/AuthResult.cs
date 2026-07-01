using AccessTrust.Web.Models;

namespace AccessTrust.Web.Services.Auth;

public class AuthResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public Usuario? Usuario { get; set; }

    public static AuthResult Ok(Usuario usuario)
    {
        return new AuthResult
        {
            Success = true,
            Usuario = usuario,
            Message = "Autenticación correcta."
        };
    }

    public static AuthResult Fail(string message)
    {
        return new AuthResult
        {
            Success = false,
            Message = message
        };
    }
}