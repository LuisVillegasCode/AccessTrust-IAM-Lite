namespace AccessTrust.Web.Models;

public enum EstadoCuenta
{
    Activo,
    Inactivo,
    Bloqueado
}

public enum SensibilidadRecurso
{
    Baja,
    Media,
    Alta
}

public enum EstadoSolicitud
{
    Pendiente,
    Aprobada,
    Rechazada
}

public enum EstadoCredencial
{
    Activa,
    Expirada,
    Revocada,
    Usada
}

public enum EstadoOtp
{
    Pendiente,
    Usado,
    Expirado,
    Bloqueado
}

public enum ResultadoAuditoria
{
    Exitoso,
    Fallido,
    Permitido,
    Denegado
}

public enum EstadoAlerta
{
    Abierta,
    Revisada,
    Cerrada
}

public enum SeveridadAlerta
{
    Baja,
    Media,
    Alta,
    Critica
}