using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AccessTrust.Web.Models;

public class PoliticaAcceso
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public SensibilidadRecurso Sensibilidad { get; set; }

    public int DuracionMaxMin { get; set; }

    public bool RequiereOtp { get; set; }

    public int MaxUsos { get; set; }

    public bool RequiereAprobacion { get; set; } = true;

    public int IntentosOtpMax { get; set; } = 3;
}