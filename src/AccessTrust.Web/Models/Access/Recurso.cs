using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AccessTrust.Web.Models;

public class Recurso
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Tipo { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public SensibilidadRecurso Sensibilidad { get; set; }

    public bool Activo { get; set; } = true;

    [BsonRepresentation(BsonType.ObjectId)]
    public string? ResponsableId { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? PoliticaId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}