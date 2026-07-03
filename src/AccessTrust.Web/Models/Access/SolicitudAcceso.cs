using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AccessTrust.Web.Models;

public class SolicitudAcceso
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonRepresentation(BsonType.ObjectId)]
    public string UsuarioId { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.ObjectId)]
    public string RecursoId { get; set; } = string.Empty;

    public string Motivo { get; set; } = string.Empty;

    public int DuracionSolicitadaMin { get; set; }
    
    [BsonIgnoreIfNull]
    public int? DuracionAprobadaMin { get; set; }

    public string Prioridad { get; set; } = "Normal";

    [BsonRepresentation(BsonType.String)]
    public EstadoSolicitud Estado { get; set; } = EstadoSolicitud.Pendiente;

    [BsonRepresentation(BsonType.ObjectId)]
    public string? AprobadorId { get; set; }

    public string? Observacion { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedAt { get; set; }
}