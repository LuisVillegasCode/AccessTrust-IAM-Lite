using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AccessTrust.Web.Models;

public class Usuario
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Correo { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = new();

    [BsonRepresentation(BsonType.String)]
    public EstadoCuenta Estado { get; set; } = EstadoCuenta.Activo;

    public int FailedLoginCount { get; set; } = 0;

    public DateTime? LockedUntil { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}