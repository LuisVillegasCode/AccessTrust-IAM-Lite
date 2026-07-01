using AccessTrust.Web.Settings;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
{
    var settings = builder.Configuration
        .GetSection("MongoDbSettings")
        .Get<MongoDbSettings>();

    if (settings is null || string.IsNullOrWhiteSpace(settings.ConnectionString))
    {
        throw new InvalidOperationException("La configuración de MongoDB no está definida.");
    }

    return new MongoClient(settings.ConnectionString);
});

builder.Services.AddSingleton<IMongoDatabase>(serviceProvider =>
{
    var settings = builder.Configuration
        .GetSection("MongoDbSettings")
        .Get<MongoDbSettings>();

    if (settings is null || string.IsNullOrWhiteSpace(settings.DatabaseName))
    {
        throw new InvalidOperationException("El nombre de la base de datos MongoDB no está definido.");
    }

    var client = serviceProvider.GetRequiredService<IMongoClient>();
    return client.GetDatabase(settings.DatabaseName);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
