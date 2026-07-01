using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AccessTrust.Web.Models;

public class Rol
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public List<string> Permisos { get; set; } = new();

    public string Descripcion { get; set; } = string.Empty;
}