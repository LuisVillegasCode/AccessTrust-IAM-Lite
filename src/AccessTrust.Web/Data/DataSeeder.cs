using AccessTrust.Web.Models;
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;

namespace AccessTrust.Web.Data;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var database = services.GetRequiredService<IMongoDatabase>();

        var roles = database.GetCollection<Rol>(MongoCollections.Roles);
        var usuarios = database.GetCollection<Usuario>(MongoCollections.Usuarios);
        var politicas = database.GetCollection<PoliticaAcceso>(MongoCollections.PoliticasAcceso);
        var recursos = database.GetCollection<Recurso>(MongoCollections.Recursos);

        await SeedRolesAsync(roles);
        await SeedPoliticasAsync(politicas);
        await SeedUsuariosAsync(usuarios);
        await SeedRecursosAsync(recursos, usuarios, politicas);
    }

    private static async Task SeedRolesAsync(IMongoCollection<Rol> roles)
    {
        var rolesIniciales = new List<Rol>
        {
            new()
            {
                Nombre = "Solicitante",
                Descripcion = "Usuario que solicita accesos temporales.",
                Permisos = new List<string> { "solicitudes.crear", "solicitudes.ver_propias", "accesos.usar" }
            },
            new()
            {
                Nombre = "Aprobador",
                Descripcion = "Usuario que revisa, aprueba o rechaza solicitudes.",
                Permisos = new List<string> { "solicitudes.revisar", "solicitudes.aprobar", "solicitudes.rechazar", "credenciales.revocar" }
            },
            new()
            {
                Nombre = "Administrador",
                Descripcion = "Usuario con administración general y funciones de auditoría.",
                Permisos = new List<string> { "usuarios.gestionar", "recursos.gestionar", "politicas.gestionar", "auditoria.ver", "alertas.ver", "reportes.ver" }
            }
        };

        foreach (var rol in rolesIniciales)
        {
            var existe = await roles.Find(r => r.Nombre == rol.Nombre).AnyAsync();
            if (!existe)
            {
                await roles.InsertOneAsync(rol);
            }
        }
    }

    private static async Task SeedPoliticasAsync(IMongoCollection<PoliticaAcceso> politicas)
    {
        var politicasIniciales = new List<PoliticaAcceso>
        {
            new()
            {
                Nombre = "Política Baja",
                Sensibilidad = SensibilidadRecurso.Baja,
                DuracionMaxMin = 480,
                RequiereOtp = false,
                MaxUsos = 20,
                RequiereAprobacion = true,
                IntentosOtpMax = 3
            },
            new()
            {
                Nombre = "Política Media",
                Sensibilidad = SensibilidadRecurso.Media,
                DuracionMaxMin = 240,
                RequiereOtp = false,
                MaxUsos = 15,
                RequiereAprobacion = true,
                IntentosOtpMax = 3
            },
            new()
            {
                Nombre = "Política Alta",
                Sensibilidad = SensibilidadRecurso.Alta,
                DuracionMaxMin = 120,
                RequiereOtp = true,
                MaxUsos = 10,
                RequiereAprobacion = true,
                IntentosOtpMax = 3
            }
        };

        foreach (var politica in politicasIniciales)
        {
            var existe = await politicas.Find(p => p.Sensibilidad == politica.Sensibilidad).AnyAsync();
            if (!existe)
            {
                await politicas.InsertOneAsync(politica);
            }
        }
    }

    private static async Task SeedUsuariosAsync(IMongoCollection<Usuario> usuarios)
    {
        var hasher = new PasswordHasher<Usuario>();

        await CrearUsuarioSiNoExisteAsync(
            usuarios,
            hasher,
            "Administrador AccessTrust",
            "admin@accesstrust.local",
            "Admin123*",
            new List<string> { "Administrador" }
        );

        await CrearUsuarioSiNoExisteAsync(
            usuarios,
            hasher,
            "Aprobador AccessTrust",
            "aprobador@accesstrust.local",
            "Aprobador123*",
            new List<string> { "Aprobador" }
        );

        await CrearUsuarioSiNoExisteAsync(
            usuarios,
            hasher,
            "Solicitante AccessTrust",
            "solicitante@accesstrust.local",
            "Solicitante123*",
            new List<string> { "Solicitante" }
        );
    }

    private static async Task CrearUsuarioSiNoExisteAsync(
        IMongoCollection<Usuario> usuarios,
        PasswordHasher<Usuario> hasher,
        string nombre,
        string correo,
        string passwordPlano,
        List<string> roles)
    {
        var existe = await usuarios.Find(u => u.Correo == correo).AnyAsync();
        if (existe)
        {
            return;
        }

        var usuario = new Usuario
        {
            Nombre = nombre,
            Correo = correo,
            Roles = roles,
            Estado = EstadoCuenta.Activo,
            FailedLoginCount = 0,
            CreatedAt = DateTime.UtcNow
        };

        usuario.PasswordHash = hasher.HashPassword(usuario, passwordPlano);

        await usuarios.InsertOneAsync(usuario);
    }

    private static async Task SeedRecursosAsync(
        IMongoCollection<Recurso> recursos,
        IMongoCollection<Usuario> usuarios,
        IMongoCollection<PoliticaAcceso> politicas)
    {
        var admin = await usuarios.Find(u => u.Correo == "admin@accesstrust.local").FirstOrDefaultAsync();

        var politicaBaja = await politicas.Find(p => p.Sensibilidad == SensibilidadRecurso.Baja).FirstOrDefaultAsync();
        var politicaMedia = await politicas.Find(p => p.Sensibilidad == SensibilidadRecurso.Media).FirstOrDefaultAsync();
        var politicaAlta = await politicas.Find(p => p.Sensibilidad == SensibilidadRecurso.Alta).FirstOrDefaultAsync();

        var recursosIniciales = new List<Recurso>
        {
            new()
            {
                Nombre = "Manual interno de procedimientos",
                Tipo = "Documento",
                Sensibilidad = SensibilidadRecurso.Baja,
                Activo = true,
                ResponsableId = admin?.Id,
                PoliticaId = politicaBaja?.Id,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Nombre = "Reporte operativo mensual",
                Tipo = "Reporte",
                Sensibilidad = SensibilidadRecurso.Media,
                Activo = true,
                ResponsableId = admin?.Id,
                PoliticaId = politicaMedia?.Id,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Nombre = "Panel restringido de clientes",
                Tipo = "Panel",
                Sensibilidad = SensibilidadRecurso.Alta,
                Activo = true,
                ResponsableId = admin?.Id,
                PoliticaId = politicaAlta?.Id,
                CreatedAt = DateTime.UtcNow
            }
        };

        foreach (var recurso in recursosIniciales)
        {
            var existe = await recursos.Find(r => r.Nombre == recurso.Nombre).AnyAsync();
            if (!existe)
            {
                await recursos.InsertOneAsync(recurso);
            }
        }
    }
}