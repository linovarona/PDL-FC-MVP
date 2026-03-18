using FichaCosto.Repositories.Implementations;
using FichaCosto.Repositories.Interfaces;
using FichaCosto.Service.Data;
using FichaCosto.Service.Services.Implementations;
using FichaCosto.Service.Services.Interfaces;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Hour /*.Day*/)
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database - Inicialización manual en startup
builder.Services.AddSingleton<DatabaseInitializer>();

//  Repositories (Fase 3)
builder.Services.AddSingleton<IConnectionFactory, SqliteConnectionFactory>();
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IFichaRepository, FichaRepository>();

// Services (Fase 4) - AGREGAR ESTAS LÍNEAS
builder.Services.AddScoped<IValidadorFichaService, ValidadorFichaService>();
builder.Services.AddScoped<ICalculadoraCostoService, CalculadoraCostoService>();
builder.Services.AddScoped<IExcelService, ExcelService>();

var app = builder.Build();

// Inicializar base de datos al arrancar
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// En Program.cs, agregar temporalmente:
if (args.Contains("--test-excel"))
{
    using var scope = app.Services.CreateScope();
    var excelService = scope.ServiceProvider.GetRequiredService<IExcelService>();
    var plantilla = await excelService.GenerarPlantillaAsync();
    await File.WriteAllBytesAsync("test-plantilla.xlsx", ((MemoryStream)plantilla).ToArray());
    Console.WriteLine("Plantilla generada: test-plantilla.xlsx");
    return;
}

app.UseAuthorization();
app.MapControllers();
app.Run();