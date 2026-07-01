using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AccessTrust.Web.Controllers;

[Route("diagnostico")]
public class DiagnosticsController : Controller
{
    private readonly IMongoDatabase _database;

    public DiagnosticsController(IMongoDatabase database)
    {
        _database = database;
    }

    [HttpGet("mongodb")]
    public async Task<IActionResult> MongoDb()
    {
        try
        {
            await _database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1));

            return Content(
                $"Conexión MongoDB OK. Base de datos: {_database.DatabaseNamespace.DatabaseName}"
            );
        }
        catch (Exception ex)
        {
            return Content($"Error de conexión a MongoDB: {ex.Message}");
        }
    }
}