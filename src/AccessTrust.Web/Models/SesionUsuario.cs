using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AccessTrust.Web.Models;

public class SesionUsuario
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string UsuarioId { get; set; } = string.Empty;

    public string Estado { get; set; } = "Activa";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public string? IpSimulada { get; set; }

    public string? UserAgent { get; set; }
}