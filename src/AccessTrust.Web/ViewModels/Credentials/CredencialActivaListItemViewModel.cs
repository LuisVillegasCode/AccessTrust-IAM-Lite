using AccessTrust.Web.Models;

namespace AccessTrust.Web.ViewModels.Credentials;

public class CredencialActivaListItemViewModel
{
    public string CredencialId { get; set; } = string.Empty;

    public string UsuarioId { get; set; } = string.Empty;

    public string RecursoId { get; set; } = string.Empty;

    public string RecursoNombre { get; set; } = string.Empty;

    public string RecursoTipo { get; set; } = string.Empty;

    public SensibilidadRecurso RecursoSensibilidad { get; set; }

    public EstadoCredencial Estado { get; set; }

    public DateTime IssuedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public int UsosRealizados { get; set; }

    public int MaxUsos { get; set; }

    public bool TieneUrlExterna { get; set; }
    
    public bool PuedeRevocar =>
        Estado == EstadoCredencial.Activa &&
        ExpiresAt > DateTime.UtcNow &&
        UsosRealizados < MaxUsos;
}