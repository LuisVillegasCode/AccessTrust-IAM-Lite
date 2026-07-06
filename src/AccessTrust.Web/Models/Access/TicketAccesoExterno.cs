using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AccessTrust.Web.Models;

public class TicketAccesoExterno
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string UsuarioId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string CredencialId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string RecursoId { get; set; } = string.Empty;

    public string TicketHash { get; set; } = string.Empty;

    public string UrlExterna { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public EstadoTicketAccesoExterno Estado { get; set; } = EstadoTicketAccesoExterno.Activo;

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    [BsonIgnoreIfNull]
    public DateTime? UsedAt { get; set; }

    [BsonIgnoreIfNull]
    public DateTime? RevokedAt { get; set; }

    [BsonIgnoreIfNull]
    public string? CreatedByIp { get; set; }

    [BsonIgnoreIfNull]
    public string? UserAgent { get; set; }
}