using AccessTrust.Web.Models;

namespace AccessTrust.Web.Services.Users;

public interface IUsuarioAdminService
{
    Task<List<Usuario>> ListarUsuariosAsync();
}