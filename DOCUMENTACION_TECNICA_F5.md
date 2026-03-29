# ACTUALIZACIÓN DE DOCUMENTACIÓN TÉCNICA
## 3 ARQUITECTURA DEL SISTEMA (Actualizada Fase 5)
### 3.1 Arquitectura General (MVP Completo)

```
┌───────────────────────────────────────────────────────────────────┐
│                        CAPA DE PRESENTACIÓN                       │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │  Excel 365 / VBA (Opcional - Fase 7)                        │  │
│  │  - Macros HTTP para consumo de API                          │  │
│  │  - Plantillas con estructura oficial                        │  │
│  └──────────────────────┬──────────────────────────────────────┘  │
│                         │ HTTP/REST (localhost:5000)              │
├─────────────────────────▼─────────────────────────────────────────┤
│                      CAPA DE SERVICIO (API)                       │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │  FichaCosto.Service (ASP.NET Core Web API)                   │ │
│  │  ┌─────────────────────────────────────────────────────────┐ │ │
│  │  │  Controllers (6)                                        │ │ │
│  │  │  - CostosController (calcular, validar, formulas)       │ │ │
│  │  │  - ExcelController (plantilla, importar, exportar)      │ │ │
│  │  │  - ClientesController (CRUD)                            │ │ │
│  │  │  - ProductosController (CRUD + MP + MO)                 │ │ │
│  │  │  - FichasController (historial, persistencia)           │ │ │
│  │  │  - ConfiguracionController (catálogos)                  │ │ │
│  │  └─────────────────────────────────────────────────────────┘ │ │
│  │  ┌─────────────────────────────────────────────────────────┐ │ │
│  │  │  Servicios de Negocio (Fase 4)                          │ │ │
│  │  │  - ICalculadoraCostoService                             │ │ │
│  │  │  - IValidadorFichaService                               │ │ │
│  │  │  - IExcelService                                        │ │ │
│  │  └─────────────────────────────────────────────────────────┘ │ │
│  │  ┌─────────────────────────────────────────────────────────┐ │ │
│  │  │  Repositorios (Fase 3)                                  │ │ │
│  │  │  - IClienteRepository, IProductoRepository, etc.        │ │ │
│  │  └─────────────────────────────────────────────────────────┘ │ │
│  └──────────────────────────────────────────────────────────────┘ │
├─────────────────────────┬─────────────────────────────────────────┤
│                    CAPA DE DATOS                                  │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │  SQLite + Dapper 2.1.66                                     │  │
│  │  - fichacosto.db (datos locales)                            │  │
│  │  - Schema SQL versionado                                    │  │
│  │  - DatabaseInitializer (migraciones manuales)               │  │
│  └─────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────┘
```

### 3.2 Flujo de Datos en la Arquitectura

```
HTTP Request → Controller → DTO → Service → Repository → Entity → Dapper → SQLite
                    ↓           ↑
              Validación    Mapping
               (Fluent)    Entity→DTO
```

### 3.3 Patrones Arquitectónicos Aplicados

| Patrón | Implementación | Ubicación |
|--------|---------------|-----------|
| **Repository** 			| Acceso a datos desacoplado 			| `Repositories/` |
| **DTO** 					| Transferencia de datos entre capas 	| `DTOs/` |
| **Mapping** 				| Conversión Entity↔DTO 				| `Mappings/` |
| **Dependency Injection** 	| Inyección de repositorios y servicios | `Program.cs` |
| **Integration Testing** 	| Tests con BD en memoria 				| `Tests/Controllers/` |

---

## ESTRUCTURA DEL PROYECTO (Actualizada Fase 5)

```
FichaCosto/
├── src/
│   └── FichaCosto.Service/
│       ├── Controllers/              # 6 controllers, 28 endpoints
│       │   ├── ApiControllerBase.cs
│       │   ├── CostosController.cs
│       │   ├── ExcelController.cs
│       │   ├── ClientesController.cs
│       │   ├── ProductosController.cs
│       │   ├── FichasController.cs
│       │   └── ConfiguracionController.cs
│       │
│       ├── DTOs/                     # Data Transfer Objects
│       │   ├── ClienteDto.cs         # Nuevo Fase 5
│       │   ├── ProductoDto.cs        # Nuevo Fase 5
│       │   ├── MateriaPrimaDto.cs    # Nuevo Fase 5
│       │   ├── FichaCostoDto.cs
│       │   ├── ResultadoCalculoDto.cs
│       │   └── ResultadoValidacionDto.cs
│       │
│       ├── Mappings/                 # Conversión Entity↔DTO
│       │   └── EntityToDtoMappings.cs
│       │
│       ├── Models/
│       │   ├── Entities/             # Clases para SQLite
│       │   │   ├── Cliente.cs
│       │   │   ├── Producto.cs
│       │   │   ├── FichaCosto.cs
│       │   │   ├── MateriaPrima.cs
│       │   │   └── ManoObraDirecta.cs
│       │   │
│       │   └── Enums/
│       │       ├── UnidadMedida.cs
│       │       ├── TipoCosto.cs
│       │       └── EstadoValidacion.cs
│       │
│       ├── Repositories/
│       │   ├── Interfaces/
│       │   │   ├── IClienteRepository.cs
│       │   │   ├── IProductoRepository.cs
│       │   │   ├── IFichaRepository.cs
│       │   │   └── IConnectionFactory.cs
│       │   │
│       │   └── Implementations/
│       │       ├── ClienteRepository.cs
│       │       ├── ProductoRepository.cs    # +GetByIdWithDetailsAsync
│       │       ├── FichaRepository.cs
│       │       └── SqliteConnectionFactory.cs
│       │
│       ├── Services/
│       │   ├── Interfaces/
│       │   │   ├── ICalculadoraCostoService.cs
│       │   │   ├── IValidadorFichaService.cs
│       │   │   └── IExcelService.cs
│       │   │
│       │   └── Implementations/
│       │       ├── CalculadoraCostoService.cs
│       │       ├── ValidadorFichaService.cs
│       │       └── ExcelService.cs
│       │
│       ├── Data/
│       │   ├── DatabaseInitializer.cs
│       │   └── Schema.sql
│       │
│       ├── Program.cs                # Configuración Web API + Swagger
│       └── FichaCosto.Service.csproj
│
├── tests/
│   └── FichaCosto.Service.Tests/
│       ├── Controllers/              # 43 tests de integración
│       │   ├── ControllerIntegrationTestsBase.cs
│       │   ├── ClientesControllerIntegrationTests.cs
│       │   ├── ProductosControllerIntegrationTests.cs
│       │   ├── FichasControllerIntegrationTests.cs
│       │   ├── CostosControllerIntegrationTests.cs
│       │   ├── ExcelControllerIntegrationTests.cs
│       │   └── ConfiguracionControllerIntegrationTests.cs
│       │
│       ├── RepositorySharedTests.cs  # Tests de repositorios (Fase 3)
│       ├── NonDisposableConnection.cs
│       ├── TestConnectionFactory.cs
│       └── DtoTests.cs
│
└── docs/
    ├── DOCUMENTACION_TECNICA.md    # Este documento
    ├── RESUMEN-05.md               # Contexto Fase 5
    └── PROCEDIMIENTO-FASE-05.md    # Guía detallada
```

---

## IMPLEMENTACIÓN DE CONTROLLERS (Fase 5)

### Principios Aplicados

1. **Separación de concerns**: Controllers solo coordinan, lógica en Services
2. **DTOs obligatorios**: Nunca se exponen Entities directamente
3. **Validación temprana**: FluentValidation + DataAnnotations
4. **Documentación automática**: Swagger annotations en cada endpoint
5. **Testabilidad**: Interfaz clara para mocks en tests

### Ejemplo de Implementación Típica

```csharp
// Controller recibe DTO, llama a Service, retorna DTO
[HttpPost("calcular")]
public async Task<ActionResult<ResultadoCalculoDto>> Calcular([FromBody] FichaCostoDto request)
{
    // 1. Validación automática (DataAnnotations)
    // 2. Llamada a servicio de negocio
    var resultado = await _calculadora.CalcularAsync(request);
    // 3. Retorno de DTO (nunca Entity)
    return Ok(resultado);
}
```

---

## PRUEBAS DE INTEGRACIÓN (Fase 5)

### Arquitectura de Tests

```
┌───────────────────────────────────────────────────┐
│  Test Class (xUnit)                           	│
│  - Hereda de ControllerIntegrationTestsBase   	│
│  - IClassFixture no necesaria (patrón manual) 	│
├───────────────────────────────────────────────────┤
│  ControllerIntegrationTestsBase         			│
│  - Crea SqliteConnection (:memory:)    			│
│  - Ejecuta Schema.sql                    			│
│  - Crea TestConnectionFactory            			│
│  - Inicializa Repositorios y Services    			│
├───────────────────────────────────────────────────┤
│  TestConnectionFactory                  			│
│  - Envuelve conexión en NonDisposableConnection 	│
│  - Misma conexión durante todo el test   			│
├───────────────────────────────────────────────────┤
│  NonDisposableConnection                			│
│  - Ignora Dispose()                      			│
│  - Mantiene BD viva entre operaciones    			│
└───────────────────────────────────────────────────┘
```

### Patrón de Test Típico

```csharp
[Fact]
public async Task Crear_Valido_RetornaCreated()
{
    // Arrange: Crear datos de prueba
    var dto = new ClienteDto { ... };
    
    // Act: Llamar directamente al controller (sin HTTP)
    var result = await _controller.Crear(dto);
    
    // Assert: Verificar resultado y estado en BD
    var created = Assert.IsType<CreatedAtActionResult>(result.Result);
    var creado = Assert.IsType<ClienteDto>(created.Value);
    Assert.True(creado.Id > 0);
    
    // Verificar persistencia
    var desdeBD = await _clienteRepo.GetByIdAsync(creado.Id);
    Assert.NotNull(desdeBD);
}
```

### Ventajas del Patrón Adoptado

| Aspecto | Ventaja |
|---------|---------|
| **Velocidad** 		| Sin overhead HTTP, tests en ms |
| **Aislamiento** 		| Cada test tiene BD limpia en memoria |
| **Determinismo** 		| Datos de prueba controlados, sin side effects |
| **Debuggabilidad** 	| Llamada directa, stack trace claro |
| **Consistencia** 		| Mismo patrón que RepositorySharedTests |

---

## CONFIGURACIÓN DE SWAGGER (Fase 5)

### Configuración en Program.cs

```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FichaCosto Service API",
        Version = "v1",
        Description = "API para automatización de fichas de costo",
        Contact = new OpenApiContact
        {
            Name = "PDL Solutions",
            Email = "soporte@pdl.cu"
        }
    });
    
    // Incluir comentarios XML
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});
```

### Anotaciones en Controllers

```csharp
[HttpPost("calcular")]
[SwaggerOperation(
    Summary = "Calcular ficha de costo",
    Description = "Calcula costos directos, precio de venta y valida margen del 30%",
    OperationId = "CalcularFicha"
)]
[SwaggerResponse(200, "Cálculo exitoso", typeof(ResultadoCalculoDto))]
[SwaggerResponse(400, "Datos inválidos")]
public async Task<ActionResult<ResultadoCalculoDto>> Calcular([FromBody] FichaCostoDto request)
```

---

## PRÓXIMA FASE (Fase 6 - Windows Service)

### Transición de Console a Service

| Modo Actual | Modo Objetivo Fase 6 |
|-------------|----------------------|
| `dotnet run` (console) | Servicio Windows (SCM) |
| Puerto 5000 hardcoded  | Configurable via appsettings |
| Logs en consola        | Logs en archivo (Serilog) |
| Ejecución manual       | Inicio automático con Windows |

### Arquitectura Final Esperada

```
┌──────────────────────────────────────────┐
│         Windows Service Host             │
│  ┌─────────────────────────────────────┐ │
│  │  FichaCosto.Service.exe             │ │
│  │  (Self-contained .NET 8.0)          │ │
│  │                                     │ │
│  │  ┌─────────────────────────────┐    │ │
│  │  │  WebHost (Kestrel)          │    │ │
│  │  │  - Puerto 5000 (HTTP)       │    │ │
│  │  │  - Puerto 5001 (HTTPS opt)  │    │ │
│  │  └─────────────────────────────┘    │ │
│  │                                     │ │
│  │  ┌─────────────────────────────┐    │ │
│  │  │  Worker Service (opcional)  │    │ │
│  │  │  - Tareas programadas       │    │ │
│  │  │  - Limpieza de logs         │    │ │
│  │  └─────────────────────────────┘    │ │
│  └─────────────────────────────────────┘ │
└──────────────────────────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│           SQLite Local                  │
│  - C:\ProgramData\FichaCosto\data\      │
│    fichacosto.db                        │
│  - C:\ProgramData\FichaCosto\logs\      │
│    log-YYYYMMDD.txt                     │
└─────────────────────────────────────────┘
```



## RESUMEN DE VERSIONES Y FASES

| Fase | Versión | Entregable Principal | Estado |
|------|---------|---------------------|--------|
| 1 | v0.1.0 | Configuración proyecto + Windows Service base | ✅ |
| 2 | v0.2.0 | Modelos de datos + SQLite + Dapper | ✅ |
| 3 | v0.3.0 | Repositorios + IConnectionFactory | ✅ |
| 4 | v0.4.0 | Servicios de negocio (Cálculo, Validación, Excel) | ✅ |
| **5** | **v0.5.0** | **API REST Controllers + 43 tests** | **✅** |
| 6 | v0.6.0 | Windows Service + Instalador MSI | 🔄 Próximo |
| 7 | v0.7.0 | Macros Excel VBA + Anexos oficiales | ⏳ Post-MVP |
| 1.0 | v1.0.0 | MVP Completo productivo | ⏳ Release |

---

**Documentación técnica actualizada:** Marzo 2026  
**Compatible con:** .NET 9.0, Windows 10/11, SQLite 3, Dapper 2.1.66