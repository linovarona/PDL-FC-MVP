using FichaCosto.Repositories.Implementations;
using FichaCosto.Repositories.Interfaces;
using FichaCosto.Service.Data; // AGREGAR ESTO AL INICIO
using Serilog;

// Configurar zona horaria por defecto
AppContext.SetSwitch("System.Globalization.Invariant", false);
TimeZoneInfo.ClearCachedData();

var builder = WebApplication.CreateBuilder(args);


// Reemplazar la configuración de Serilog por esta:

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ZonaHoraria", TimeZoneInfo.Local.DisplayName) // Opcional
    .WriteTo.Console(
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
    )
    .WriteTo.File(
        path: Path.Combine(AppContext.BaseDirectory, "Logs", "log-.txt"),
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
        // Forzar flush inmediato para debug
        buffered: false,
        shared: true
    )
    .CreateLogger();

builder.Host.UseSerilog();
builder.Host.UseWindowsService();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IFichaRepository, FichaRepository>();

// Database Initializer
builder.Services.AddSingleton<DatabaseInitializer>(); // <-- AGREGADO

// CORS para Excel client
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowExcelClient", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowExcelClient");
app.UseAuthorization();
app.MapControllers();

// Crear directorios necesarios
var paths = new[] { "Data", "Logs", "Exportaciones", "Plantillas" };
foreach (var path in paths)
{
    var fullPath = Path.Combine(AppContext.BaseDirectory, path);
    if (!Directory.Exists(fullPath))
        Directory.CreateDirectory(fullPath);
}

// Inicializar base de datos <-- AGREGADO
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}

app.Run();