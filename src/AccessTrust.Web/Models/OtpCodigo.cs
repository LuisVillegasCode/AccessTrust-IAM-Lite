using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AccessTrust.Web.Models;

public class OtpCodigo
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string UsuarioId { get; set; } = string.Empty;

    public string Accion { get; set; } = string.Empty;

    public string CodigoHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public int Intentos { get; set; } = 0;

    [BsonRepresentation(BsonType.String)]
    public EstadoOtp Estado { get; set; } = EstadoOtp.Pendiente;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}