using FichaCosto.Repositories.Implementations;
using FichaCosto.Repositories.Interfaces;
using FichaCosto.Service.Data;
using FichaCosto.Service.Services;
using FichaCosto.Service.Services.Implementations;
using FichaCosto.Service.Services.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// CONFIGURACIÓN DE SERVICIOS (DI)
// ========================================

// Logging con Serilog
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Controllers y API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger/OpenAPI (Fase 5)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo //.Models.OpenApiInfo
    {
        Title = "FichaCosto Service API",
        Version = "v1",
        Description = "API para automatización de fichas de costo según Res. 148 / 2023 y 209 / 2024",
        Contact = new Microsoft.OpenApi.OpenApiContact // .Models.OpenApiContact
        {
            Name = "PDL Solutions",
            Email = "soporte@pdl.cu"
        }
    });

    // Habilitar anotaciones XML para documentación
    var xmlFile = $"{ System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// CORS para Excel/Clientes externos
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowExcelClient", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ========================================
// SERVICIOS DE APLICACIÓN (Fases 3 y 4)
// ========================================

// Database Context
//builder.Services.AddSingleton<FichaCostoContext>();
builder.Services.AddSingleton<DatabaseInitializer>();


// Repositorios (Fase 3)
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IFichaRepository, FichaRepository>();
//builder.Services.AddScoped<ICostoIndirectoRepository, CostoIndirectoRepository>();
//builder.Services.AddScoped<IGastoGeneralRepository, GastoGeneralRepository>();
//builder.Services.AddScoped<IMateriaPrimaRepository, MateriaPrimaRepository>();
//builder.Services.AddScoped<IManoObraRepository, ManoObraRepository>();

// Servicios de Negocio (Fase 4)
builder.Services.AddScoped<ICalculadoraCostoService, CalculadoraCostoService>();
builder.Services.AddScoped<IValidadorFichaService, ValidadorFichaService>();
builder.Services.AddScoped<IExcelService, ExcelService>();

// ========================================
// CONSTRUIR APLICACIÓN
// ========================================

var app = builder.Build();

// Inicializar base de datos
using (var scope = app.Services.CreateScope())
{
    //var context = scope.ServiceProvider.GetRequiredService<FichaCostoContext>();
    //DatabaseInitializer.Initialize(context);
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();

}

// ========================================
// PIPELINE HTTP
// ========================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint(" / swagger / v1 / swagger.json", "FichaCosto API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowExcelClient");
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet(" / api / health", () => new { status = "OK", timestamp = DateTime.UtcNow });

// ========================================
// INICIAR SERVICIO
// ========================================

Log.Information("Iniciando FichaCosto Service - Puerto 5000 / 5001");
app.Run();