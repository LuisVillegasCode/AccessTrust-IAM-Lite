namespace AccessTrust.Web.Services.Auth;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string correo, string password);
}