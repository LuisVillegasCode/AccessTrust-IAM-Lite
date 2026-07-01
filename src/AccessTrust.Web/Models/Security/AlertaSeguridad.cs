using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AccessTrust.Web.Models;

public class AlertaSeguridad
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Tipo { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public SeveridadAlerta Severidad { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? UsuarioId { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? RecursoId { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public EstadoAlerta Estado { get; set; } = EstadoAlerta.Abierta;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }
}