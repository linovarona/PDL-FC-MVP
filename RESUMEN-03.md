Kimi: Entendido. Aquí está el `RESUMEN-03.md` con los aspectos más relevantes y el commit para cierre de fase.

---

## 📄 RESUMEN-03.md

```markdown
# RESUMEN-03: Fase 3 - Repositorios (MVP) - COMPLETADA
## Proyecto PDL-FC-MVP (FichaCosto Service)

**Fecha:** Marzo 2026  
**Estado:** ✅ **COMPLETADA**  
**Próxima Fase:** Fase 4 - Servicios de Negocio (CalculadoraCostoService)  
**Stack Confirmado:** .NET 9.0 + Dapper 2.1.66 + SQLite + IConnectionFactory

---

## 📋 CONTEXTO DE LA FASE 3

### Objetivo Logrado
Implementar capa de acceso a datos con patrón Repository usando Dapper, resolviendo el problema crítico de SQLite in-memory en tests.

### Problema Crítico Resuelto: SQLite "no such table"

| Aspecto | Problema Original | Solución Implementada |
|---------|-------------------|---------------------|
| **SQLite :memory:** | Cada conexión nueva = BD vacía nueva | Single connection compartida |
| **`using` statement** | Cierra conexión, destruye BD | `NonDisposableConnection` ignora Dispose() |
| **Tests** | Fallaban con "no such table" | Tests estables y reproducibles |
| **Producción** | Sin cambios requeridos | `SqliteConnectionFactory` crea conexiones nuevas |

---

## ✅ ENTREGABLES COMPLETADOS

### 1. Interfaces de Repositorio (3)
| Interfaz | Operaciones MVP |
|----------|-----------------|
| `IClienteRepository` | CRUD + ExistsByCuit |
| `IProductoRepository` | CRUD + GetByClienteId + GetByIdWithDetails + ExistsByCodigo |
| `IFichaRepository` | Create + GetById + GetByProductoId + GetHistorial + GetUltimaFicha + Delete |

### 2. Implementaciones con Dapper (3)
- Todas usan `IConnectionFactory` (no `IConfiguration` directamente)
- SQL parametrizado con Dapper
- Mapeo automático objeto-relacional
- Sin Entity Framework (ligereza para MVP offline)

### 3. Patrón IConnectionFactory (Nuevo)
| Implementación | Uso | Característica |
|----------------|-----|----------------|
| `SqliteConnectionFactory` | Producción | Crea conexión nueva por operación |
| `TestConnectionFactory` | Tests | Reutiliza conexión compartida |
| `NonDisposableConnection` | Tests | Wrapper que ignora Dispose() |

### 4. Tests de Integración (6 tests pasando)
| Test | Cobertura |
|------|-----------|
| `Cliente_CRUD_Completo` | Create, Read, Update, Delete, Exists, List |
| `Cliente_ExistsByCuit_DistingueExistenteYNuevo` | Validación de unicidad |
| `Producto_ConClienteYDetalles` | Relación 1:N + carga de detalles |
| `Producto_ExistsByCodigo_ValidaUnicidad` | Unicidad de código por cliente |
| `Ficha_CRUD_SinCamposNoHabilitados` | CRUD básico (sin CostosIndirectos/GastosGenerales) |
| `Ficha_HistorialMultiple` | Ordenamiento cronológico + paginación |
| `EscenarioCompleto_CrearFichaDeCosto` | Flujo end-to-end |

---

## 🔧 DECISIONES TÉCNICAS CLAVE

### 1. Dapper vs EF Core
**Decisión:** Dapper  
**Justificación:** 
- Performance superior para queries simples
- Control SQL exacto (normativas requieren precisión)
- Menor overhead en aplicación offline
- Sin migrations complejas

### 2. IConnectionFactory vs Connection String Directo
**Decisión:** IConnectionFactory  
**Justificación:**
- Abstracción permite inyección de conexiones compartidas en tests
- Producción no se ve afectada (crea conexiones nuevas)
- Tests son deterministas (misma BD durante todo el test)

### 3. SQLite In-Memory para Tests
**Decisión:** `:memory:` + `NonDisposableConnection`  
**Alternativas descartadas:**
- `:memory:;Cache=Shared` → No funciona confiablemente con Microsoft.Data.Sqlite
- Archivo temporal → Más lento, requiere limpieza de archivos
- Inyección de conexión en repositories → Modifica API de producción

---

## 📊 ESTADO DE LA BASE DE DATOS (Tests)

### Schema Verificado
| Tabla | Columnas MVP | Estado |
|-------|--------------|--------|
| `Clientes` | Id, NombreEmpresa, CUIT, Direccion, ContactoNombre, ContactoEmail, ContactoTelefono, Activo, FechaAlta | ✅ |
| `Productos` | Id, ClienteId, Codigo, Nombre, Descripcion, UnidadMedida, Activo, FechaCreacion | ✅ |
| `MateriasPrimas` | Id, ProductoId, Nombre, Cantidad, CostoUnitario, UnidadMedida, Activo | ✅ |
| `ManoObraDirecta` | Id, ProductoId, DescripcionTarea, Horas, CostoHora, CostoTotalCalculado, FechaRegistro | ✅ |
| `FichasCosto` | Id, ProductoId, FechaCalculo, CostoMateriasPrimas, CostoManoObra, **CostoTotal**, MargenUtilidad, PrecioVentaSugerido, EstadoValidacion, Observaciones, CalculadoPor | ✅ |

**Nota:** Sin `CostosIndirectos` ni `GastosGenerales` (habilitados en post-MVP v1.1)

---

## 🎯 APRENDIZAJES DE INVESTIGACIÓN

### SQLite In-Memory: Lecciones Aprendidas

```csharp
// ❌ NO FUNCIONA: Cache=Shared con múltiples conexiones
"Data Source=:memory:;Cache=Shared"  // Cada conexión con 'using' cierra y pierde BD

// ✅ FUNCIONA: Single connection con NonDisposableConnection
"Data Source=:memory:"  // Una conexión, wrapper ignora Dispose()
```

### Flujo de Conexiones en Tests

```
Test Constructor
    └── _sharedConnection.Open()  ← BD creada en memoria
        └── Ejecutar Schema SQL
            └── TestConnectionFactory(_sharedConnection)
                └── NonDisposableConnection(wrapper)
                    └── Repository.CreateConnection()
                        └── using (connection) { ... }  ← Dispose() ignorado
                            └── Múltiples operaciones...
                                └── Test.Dispose()
                                    └── _sharedConnection.Close()  ← BD destruida
```

---

## 🔧 CONFIGURACIÓN DEL ENTORNO

### URLs de Acceso Confirmadas
| Endpoint | URL | Estado |
|----------|-----|--------|
| Swagger UI | `http://localhost:5001/swagger` | ✅ Funcional |
| API Base | `http://localhost:5001/api` | ⚠️ Pendiente controllers (Fase 5) |

### Dependencias Registradas en DI (Program.cs)
```csharp
builder.Services.AddSingleton<IConnectionFactory, SqliteConnectionFactory>();
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();
builder.Services.AddScoped<IFichaRepository, FichaRepository>();
```

---

## 🚀 CONTEXTO PARA FASE 4: SERVICIOS DE NEGOCIO

### Servicios a Implementar
| Servicio | Responsabilidad | Dependencias |
|----------|-----------------|------------|
| `CalculadoraCostoService` | Σ(MateriasPrimas) + Σ(ManoObra) + Margen | Repositories |
| `ValidadorFichaService` | Validar campos + límite 30% Res.209/2024 | Calculadora |
| `ExcelService` (MVP) | Importar/Exportar datos básicos | Repositories |

### Patrón Sugerido
```
Controller → Service → Repository → SQLite
                ↓
         Validaciones de Negocio
                ↓
         Cálculos de Costos
```

---

## ⚠️ RIESGOS MITIGADOS

| Riesgo | Mitigación Aplicada |
|--------|---------------------|
| Tests flaky por SQLite | NonDisposableConnection garantiza estabilidad |
| Acoplamiento a SQLite | IConnectionFactory permite cambiar a SQL Server/PostgreSQL |
| Campos no habilitados en Ficha | Repository solo usa columnas existentes en schema |
| Concurrencia en tests | Una conexión por test = aislamiento total |

---

## 📚 DOCUMENTACIÓN GENERADA

| Documento | Ubicación | Propósito |
|-----------|-----------|-----------|
| `RESUMEN-03.md` | `docs/` | Este documento - contexto para Fase 4 |
| `PROCEDIMIENTO-FASE-03.md` | `docs/` | Guía ultra-detallada de implementación |
| `RepositorySharedTests.cs` | `tests/` | Tests de integración con explicaciones |
| `NonDisposableConnection.cs` | `tests/` | Solución técnica al problema SQLite |

---

## 🔧 COMANDOS RÁPIDOS PARA CONTINUAR

```powershell
# Ubicarse en proyecto
cd "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP"

# Compilar y verificar
dotnet build --configuration Release --no-restore

# Ejecutar tests de Fase 3
dotnet test --filter "FullyQualifiedName~RepositorySharedTests" --verbosity normal

# Verificar cobertura (deberían pasar 6/6)
dotnet test --verbosity normal
```

---

## ✅ CHECKPOINT PARA INICIAR FASE 4

Antes de comenzar Fase 4 (Servicios de Negocio), verificar:

- [ ] `dotnet build` genera 0 errores, 0 advertencias críticas
- [ ] `dotnet test` pasa 6/6 tests de RepositorySharedTests
- [ ] Servicio corre y Swagger accesible en `http://localhost:5001/swagger`
- [ ] Base de datos `fichacosto.db` se crea automáticamente
- [ ] Logs funcionan correctamente en `Logs/log-YYYYMMdd.txt`
- [ ] Repositories inyectables via IConnectionFactory

**Si todos los checks pasan → Proceder a Fase 4**

---

## 📝 NOTAS DE CIERRE

**Rama de investigación:** `investigacion/sqlite-shared-cache`  
**Merge a develop:** Completado con patrón IConnectionFactory  
**Próximo milestone:** v0.4.0 - Servicios de Cálculo y Validación

**Fin del Resumen Fase 3**

*Documento generado para continuidad del desarrollo. Próximo entregable: Fase 4 - Servicios de Negocio*
```

---

## 📝 Commit para Cierre de Fase

```powershell
cd "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP"

# Agregar todos los cambios de Fase 3
git add src/FichaCosto.Service/Repositories/Interfaces/IConnectionFactory.cs
git add src/FichaCosto.Service/Repositories/Implementations/SqliteConnectionFactory.cs
git add src/FichaCosto.Service/Repositories/Implementations/ClienteRepository.cs
git add src/FichaCosto.Service/Repositories/Implementations/ProductoRepository.cs
git add src/FichaCosto.Service/Repositories/Implementations/FichaRepository.cs
git add src/FichaCosto.Service/Data/DatabaseInitializationService.cs
git add src/FichaCosto.Service/Program.cs
git add tests/FichaCosto.Service.Tests/NonDisposableConnection.cs
git add tests/FichaCosto.Service.Tests/TestConnectionFactory.cs
git add tests/FichaCosto.Service.Tests/RepositorySharedTests.cs
git add docs/RESUMEN-03.md
git add docs/PROCEDIMIENTO-FASE-03.md

# Commit de cierre de fase
git commit -m "feat: Completa Fase 3 - Repositorios con IConnectionFactory

Implementa capa de acceso a datos con patrón Repository:
- Agrega IConnectionFactory para abstraer creación de conexiones
- Implementa SqliteConnectionFactory (producción) y TestConnectionFactory (tests)
- Crea NonDisposableConnection para resolver 'no such table' en SQLite :memory:
- Implementa ClienteRepository, ProductoRepository, FichaRepository con Dapper
- Ajusta FichaRepository a campos habilitados en MVP (sin CostosIndirectos/GastosGenerales)
- Agrega DatabaseInitializationService para inicialización automática
- Incluye 6 tests de integración estables y documentados

Resuelve problema crítico de SQLite in-memory donde cada conexión con 'using'
destruía la BD. Solución: conexión compartida con wrapper que ignora Dispose().

Refs: Fase 3 completada, preparando Fase 4 (Servicios de Negocio)
Closes: investigacion/sqlite-shared-cache"
```

---

## 🏷️ Tag Opcional

```powershell
# Crear tag de versión
git tag -a v0.3.0 -m "Fase 3: Repositorios Dapper + IConnectionFactory"

# Ver tag creado
git tag -n1
```

---

¿Necesitas ajustar algo del RESUMEN-03 o del mensaje de commit?
