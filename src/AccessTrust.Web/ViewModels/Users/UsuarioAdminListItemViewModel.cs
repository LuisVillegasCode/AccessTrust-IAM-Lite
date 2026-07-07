namespace AccessTrust.Web.ViewModels.Users;

public class UsuarioAdminListItemViewModel
{
    public string Id { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Correo { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = new();

    public string Estado { get; set; } = string.Empty;

    public string DocumentoIdentidadDescifrado { get; set; } = string.Empty;

    public bool TieneDocumentoCifrado { get; set; }

    public DateTime CreatedAt { get; set; }
}