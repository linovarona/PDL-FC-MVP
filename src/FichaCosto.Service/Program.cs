// src/FichaCosto.Service/Program.cs
using FichaCosto.Repositories.Implementations;
using FichaCosto.Repositories.Interfaces;
using FichaCosto.Service.Data;
using FichaCosto.Service.Services.Implementations;
using FichaCosto.Service.Services.Interfaces;
using Microsoft.Extensions.Hosting.WindowsServices;
using Serilog;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// ========== CONFIGURACION SERILOG ==========
var baseDir = AppContext.BaseDirectory;
var isWindowsService = WindowsServiceHelpers.IsWindowsService();

// Siempre usar ProgramData para datos (Logs, SQLite) - Program Files tiene permisos restrictivos
var dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "FichaCostoService");
var dbDir = Path.Combine(dataDir, "Data");
var logsPath = Path.Combine(dataDir, "Logs");

foreach (var dir in new[] { dataDir, dbDir, logsPath })
{
    if (!Directory.Exists(dir))
    {
        Directory.CreateDirectory(dir);
    }
}

// Configurar connection string para usar ProgramData\Data
var connStr = $"Data Source={Path.Combine(dbDir, "fichacosto.db")};Cache=Shared";
builder.Configuration["ConnectionStrings:DefaultConnection"] = connStr;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(logsPath, "log-.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        fileSizeLimitBytes: 10485760,
        rollOnFileSizeLimit: true,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

Log.Information("FichaCosto Service iniciando. DataDir: {DataDir}, LogsDir: {LogsDir}", dbDir, logsPath);

builder.Host.UseSerilog();

// ========== DETECCION WINDOWS SERVICE ==========
if (isWindowsService)
{
    builder.Host.UseWindowsService(options =>
    {
        options.ServiceName = "FichaCostoService";
    });
    builder.Host.UseContentRoot(AppContext.BaseDirectory);
}

// ========== SERVICIOS MVC ==========
builder.Services.AddControllers().AddApplicationPart(Assembly.GetExecutingAssembly()); // <-- AGREGAR ESTO;
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
builder.Services.AddSingleton<IConnectionFactory, SqliteConnectionFactory>();
builder.Services.AddSingleton<DatabaseInitializer>();

// Otros servicios
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IFichaRepository, FichaRepository>();
builder.Services.AddScoped<ICalculadoraCostoService, CalculadoraCostoService>();
builder.Services.AddScoped<IValidadorFichaService, ValidadorFichaService>();
builder.Services.AddScoped<IExcelService, ExcelService>();

var app = builder.Build();

// ========== INICIALIZACION BASE DE DATOS ==========
try
{
    using (var scope = app.Services.CreateScope())
    {
        var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
        await initializer.InitializeAsync();
        Log.Information("DatabaseInitializer completado exitosamente");
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "FALLA CRITICA: No se pudo inicializar la base de datos");
    // Continuar para permitir diagnostico via API
}

// ========== MIDDLEWARE ==========
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FichaCosto API v1.0.0-MVP");
    c.RoutePrefix = "swagger";
});

app.UseAuthorization();

// LOGGING: Listar todos los endpoints registrados
Log.Information("=== ENDPOINTS REGISTRADOS ===");
foreach (var endpoint in app.Services.GetRequiredService<Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider>()
    .ActionDescriptors.Items)
{
    if (endpoint is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor controllerAction)
    {
        Log.Information("Controller: {Controller}, Action: {Action}, Route: {Route}",
            controllerAction.ControllerName,
            controllerAction.ActionName,
            controllerAction.AttributeRouteInfo?.Template ?? "N/A");
    }
}
Log.Information("=== FIN ENDPOINTS ===");

app.MapControllers(); // <-- IMPORTANTE: Mapea AdminController y otros controllers

// Health Check endpoint (tambien disponible en AdminController)
app.MapGet("/api/health", () => new
{
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
    Log.Fatal(ex, "Fallo critico al iniciar el servicio");
    throw;
}
finally
{
    Log.CloseAndFlush();
}