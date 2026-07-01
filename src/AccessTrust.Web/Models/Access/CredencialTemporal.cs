using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AccessTrust.Web.Models;

public class CredencialTemporal
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string UsuarioId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string RecursoId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string SolicitudId { get; set; } = string.Empty;

    public string TokenHash { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public EstadoCredencial Estado { get; set; } = EstadoCredencial.Activa;

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public int MaxUsos { get; set; }

    public int UsosRealizados { get; set; } = 0;

    public DateTime? RevokedAt { get; set; }
}