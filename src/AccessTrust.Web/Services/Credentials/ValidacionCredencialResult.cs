using AccessTrust.Web.Models;

namespace AccessTrust.Web.Services.Credentials;

public class ValidacionCredencialResult
{
    public bool Permitido { get; set; }

    public string Mensaje { get; set; } = string.Empty;

    public CredencialTemporal? Credencial { get; set; }

    public string MotivoCodigo { get; set; } = string.Empty;

    public static ValidacionCredencialResult Ok(CredencialTemporal credencial)
    {
        return new ValidacionCredencialResult
        {
            Permitido = true,
            Mensaje = "Acceso permitido.",
            Credencial = credencial,
            MotivoCodigo = "ACCESO_PERMITIDO"
        };
    }

    public static ValidacionCredencialResult Fail(string mensaje, string motivoCodigo, CredencialTemporal? credencial = null)
    {
        return new ValidacionCredencialResult
        {
            Permitido = false,
            Mensaje = mensaje,
            Credencial = credencial,
            MotivoCodigo = motivoCodigo
        };
    }
}