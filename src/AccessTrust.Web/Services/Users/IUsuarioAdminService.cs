using AccessTrust.Web.Models;
using AccessTrust.Web.ViewModels.Users;

namespace AccessTrust.Web.Services.Users;

public interface IUsuarioAdminService
{
    Task<List<Usuario>> ListarUsuariosAsync();
    
    Task<(bool Success, string Message)> CrearUsuarioAsync(
        CrearUsuarioAdminViewModel model,
        string actorUserId
    );
}