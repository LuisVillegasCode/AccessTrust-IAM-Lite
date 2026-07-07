using AccessTrust.Web.Services.Security;
using AccessTrust.Web.Services.Users;
using AccessTrust.Web.ViewModels.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessTrust.Web.Controllers;

[Authorize(Roles = "Administrador")]
public class UsuariosController : Controller
{
    private readonly IUsuarioAdminService _usuarioAdminService;
    private readonly IFieldEncryptionService _fieldEncryptionService;

    public UsuariosController(
        IUsuarioAdminService usuarioAdminService,
        IFieldEncryptionService fieldEncryptionService)
    {
        _usuarioAdminService = usuarioAdminService;
        _fieldEncryptionService = fieldEncryptionService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var usuarios = await _usuarioAdminService.ListarUsuariosAsync();

        var viewModels = usuarios.Select(usuario =>
        {
            var documentoDescifrado = string.Empty;

            if (!string.IsNullOrWhiteSpace(usuario.DocumentoIdentidadCifrado))
            {
                documentoDescifrado = _fieldEncryptionService.DecryptFromBase64(
                    usuario.DocumentoIdentidadCifrado
                );
            }

            return new UsuarioAdminListItemViewModel
            {
                Id = usuario.Id ?? string.Empty,
                Nombre = usuario.Nombre,
                Correo = usuario.Correo,
                Roles = usuario.Roles,
                Estado = usuario.Estado.ToString(),
                DocumentoIdentidadDescifrado = documentoDescifrado,
                TieneDocumentoCifrado = !string.IsNullOrWhiteSpace(usuario.DocumentoIdentidadCifrado),
                CreatedAt = usuario.CreatedAt
            };
        }).ToList();

        return View(viewModels);
    }
}