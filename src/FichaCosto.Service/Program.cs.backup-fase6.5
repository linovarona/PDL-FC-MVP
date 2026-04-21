// src/FichaCosto.Service/Program.cs
using FichaCosto.Repositories.Implementations;
using FichaCosto.Repositories.Interfaces;
using FichaCosto.Service.Data;
using FichaCosto.Service.Services.Implementations;
using FichaCosto.Service.Services.Interfaces;
using Microsoft.Extensions.Hosting.WindowsServices;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ========== CONFIGURACIÓN SERILOG ==========
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// ========== DETECCIÓN WINDOWS SERVICE ==========
if (WindowsServiceHelpers.IsWindowsService())
{
    builder.Host.UseWindowsService(options =>
    {
        options.ServiceName = "FichaCostoService";
    });
    builder.Host.UseContentRoot(AppContext.BaseDirectory);
}

// ========== SERVICIOS MVC ==========
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "FichaCosto API",
        Version = "v1.0.0-MVP",
        Description = $"Environment: {builder.Environment.EnvironmentName}"
    });
});

// ========== REPOSITORIES & DATABASE ==========
// SIEMPRE registramos la factory estándar
builder.Services.AddSingleton<IConnectionFactory, SqliteConnectionFactory>();

// DatabaseInitializer detecta automáticamente si es test por IHostEnvironment
builder.Services.AddSingleton<DatabaseInitializer>();

// Otros servicios...
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IFichaRepository, FichaRepository>();
builder.Services.AddScoped<ICalculadoraCostoService, CalculadoraCostoService>();
builder.Services.AddScoped<IValidadorFichaService, ValidadorFichaService>();
builder.Services.AddScoped<IExcelService, ExcelService>();

var app = builder.Build();

// ========== INICIALIZACIÓN BASE DE DATOS ==========
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}

// ========== MIDDLEWARE ==========
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FichaCosto API v1.0.0-MVP");
    c.RoutePrefix = "swagger";
});

app.UseAuthorization();
app.MapControllers();

// Health Check
app.MapGet("/api/health", () => new {
    status = "OK",
    timestamp = DateTime.UtcNow,
    version = "v1.0.0-MVP",
    environment = app.Environment.EnvironmentName
});

try
{
    Log.Information("Iniciando FichaCosto Service v1.0.0-MVP en [{Environment}]...",
        builder.Environment.EnvironmentName);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Fallo crítico al iniciar el servicio");
    throw;
}
finally
{
    Log.CloseAndFlush();
}