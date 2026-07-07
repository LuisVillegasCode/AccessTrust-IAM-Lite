using AccessTrust.Web.Services.Reports;
using AccessTrust.Web.Services.Users;
using AccessTrust.Web.Services.Security;
using AccessTrust.Web.Services.ExternalAccess;
using AccessTrust.Web.Services.Credentials;
using AccessTrust.Web.Services.Requests;
using AccessTrust.Web.Services.Policies;
using AccessTrust.Web.Services.Resources;
using AccessTrust.Web.Services.Audit;
using AccessTrust.Web.Services.Auth;
using AccessTrust.Web.Data;
using AccessTrust.Web.Settings;
using Microsoft.AspNetCore.Authentication.Cookies;
using MongoDB.Driver;
using AccessTrust.Web.Repositories;

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

builder.Services.AddScoped(typeof(IMongoRepository<>), typeof(MongoRepository<>));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "AccessTrust.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";

        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddSingleton<IFieldEncryptionService, AesGcmFieldEncryptionService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IUsuarioAdminService, UsuarioAdminService>();
builder.Services.AddScoped<IReporteService, ReporteService>();

builder.Services.AddScoped<IPoliticaAccesoService, PoliticaAccesoService>();
builder.Services.AddScoped<IRecursoService, RecursoService>();
builder.Services.AddScoped<ISolicitudAccesoService, SolicitudAccesoService>();
builder.Services.AddScoped<ICredencialTemporalService, CredencialTemporalService>();
builder.Services.AddScoped<IExternalAccessTicketService, ExternalAccessTicketService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await DataSeeder.SeedAsync(scope.ServiceProvider);
}

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
