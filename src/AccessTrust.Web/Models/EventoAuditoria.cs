using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AccessTrust.Web.Models;

public class EventoAuditoria
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public long Seq { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string? ActorUserId { get; set; }

    public string Accion { get; set; } = string.Empty;

    public string EntidadTipo { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string? EntidadId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public ResultadoAuditoria Resultado { get; set; }

    public Dictionary<string, string> Detalle { get; set; } = new();

    public string PrevHash { get; set; } = string.Empty;

    public string Hash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}