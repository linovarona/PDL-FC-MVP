using FichaCosto.Repositories.Implementations;
using FichaCosto.Repositories.Interfaces;
using FichaCosto.Service.Data;
//using FichaCosto.Service.Repositories;
using FichaCosto.Service.Services;
using FichaCosto.Service.Services.Implementations;
using FichaCosto.Service.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.WindowsServices;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog desde appsettings
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Detectar si corre como Windows Service
if (WindowsServiceHelpers.IsWindowsService())
{
    builder.Host.UseWindowsService(options =>
    {
        options.ServiceName = "FichaCostoService";
    });

    // Configurar ContentRoot al directorio del ejecutable (importante para servicios)
    builder.Host.UseContentRoot(AppContext.BaseDirectory);
}

// Servicios existentes
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "FichaCosto API", Version = "v1.0.0-MVP" });

    // Incluir XML docs si existen
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Database & Repositories
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddSingleton<IConnectionFactory, SqliteConnectionFactory>();


//builder.Services.AddScoped<IFichaCostoContext, FichaCostoContext>();
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IFichaRepository, FichaRepository>();

// Business Services
builder.Services.AddScoped<ICalculadoraCostoService, CalculadoraCostoService>();
builder.Services.AddScoped<IValidadorFichaService, ValidadorFichaService>();
builder.Services.AddScoped<IExcelService, ExcelService>();

var app = builder.Build();

// Asegurar creación de directorio de logs
var logsPath = Path.Combine(AppContext.BaseDirectory, "Logs");
if (!Directory.Exists(logsPath))
{
    Directory.CreateDirectory(logsPath);
}

// Initialize Database
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();

}

// Swagger siempre disponible (útil para MVP)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FichaCosto API v1.0.0-MVP");
    c.RoutePrefix = "swagger";
});

app.UseAuthorization();
app.MapControllers();

// Health Check endpoint
app.MapGet("/api/health", () => new { status = "OK", timestamp = DateTime.UtcNow, version = "v1.0.0-MVP" });

try
{
    Log.Information("Iniciando FichaCosto Service v1.0.0-MVP...");
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