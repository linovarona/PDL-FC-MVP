# RESUMEN-02: Fase 2 - Modelos de Datos y Base de Datos
## Proyecto PDL-FC-MVP (FichaCosto Service)

**Fecha:** Marzo 2026  
**Estado:** ✅ **COMPLETADA**  
**Próxima Fase:** Fase 3 - Repositorios (Acceso a datos con Dapper)  
**Stack Confirmado:** .NET 9.0 (SDK 9.0.308) + Visual Studio 2022 + SQLite + Dapper

---

## 📋 CONTEXTO DE LA FASE 2

### Objetivo Logrado
Implementar capa de modelos de datos completa: entidades, DTOs, enums, esquema SQLite e inicialización automática de base de datos con seed data.

### Ajustes Importantes Realizados

| Aspecto | Plan Original | Ajuste Real | Motivo |
|---------|---------------|-------------|--------|
| **.NET Version** 				| 8.0 | **9.0** | SDK disponible en entorno de desarrollo |
| **Puerto API** 				| 5000 | **5001 / 7048** | Configuración VS 2022 `launchSettings.json` |
| **Enum EstadoValidacion** 	| Iniciaba en 1 | **Agregado `NoDefinido = 0`** | Resolver conflicto valores por defecto |
| **Inicialización entidad** 	| Sin valor por defecto | **`= EstadoValidacion.Valido`** | Robustez en instanciación |
| **Resolución nombres** 		| - | **`using FichaCostoEntity = ...`** | Conflicto namespace vs clase |

---

## ✅ ENTREGABLES COMPLETADOS

### 1. Enums (3 creados)
| Enum | Valores | Ubicación |
|------|---------|-----------|
| `TipoCosto` | MateriaPrima=1, ManoObra=2, CostoIndirecto=3, GastosGenerales=4 | `Models/Enums/TipoCosto.cs` |
| `UnidadMedida` | Kg=1, Gr=2, L=3, Ml=4, Unidad=5, M=6, M2=7, M3=8, Hora=9 | `Models/Enums/UnidadMedida.cs` |
| `EstadoValidacion` | **NoDefinido=0**, Valido=1, Advertencia=2, Excedido=3, ErrorDatos=4 | `Models/Enums/EstadoValidacion.cs` |

### 2. Entities (5 creadas)
| Entidad | Propósito | Propiedades Calculadas |
|---------|-----------|------------------------|
| `Cliente` | Datos PyME | - |
| `Producto` | Bien/Servicio | - |
| `MateriaPrima` | Insumos | `CostoTotal = Cantidad × CostoUnitario` |
| `ManoObraDirecta` | Costo trabajo | `CostoBase`, `CostoTotal` (con cargas sociales) |
| `FichaCosto` | Resultado cálculo | - |

### 3. DTOs (3 principales + nested)
| DTO | Tipo | Validaciones |
|-----|------|--------------|
| `FichaCostoDto` | Entrada | `[Range(0, 30)]` en margen |
| `MateriaPrimaInputDto` | Nested | Cantidad > 0, Costo > 0 |
| `ManoObraInputDto` | Nested | Horas > 0, Salario > 0 |
| `ResultadoCalculoDto` | Salida | Estructura completa respuesta |
| `ResultadoValidacionDto` | Salida validación | Metadata normativa Res. 209/2024 |

### 4. Base de Datos SQLite
| Componente | Estado |
|------------|--------|
| **Schema.sql** 				| ✅ 5 tablas, 10 índices, 1 vista (`vw_ProductosUltimoCosto`) |
| **DatabaseInitializer** 		| ✅ Inicialización automática, seed data |
| **Integridad referencial** 	| ✅ FKs con `ON DELETE CASCADE` |
| **Constraints** 				| ✅ CHECK en valores numéricos, UNIQUE en CUIT |

### 5. Tests (7 pasando)
| Test | Cobertura |
|------|-----------|
| `Can_Connect_To_Sqlite` | Conexión Dapper + SQLite |
| `Can_Execute_Schema` | Ejecución SQL schema |
| `Schema_SQL_File_Exists` | Existencia archivo Schema.sql |
| `MateriaPrima_Calculates_CostoTotal` | Lógica cálculo entidad |
| `ManoObra_Calculates_CostoTotal` | Lógica cálculo con cargas sociales |
| `FichaCosto_Has_Valid_Defaults` | Valores por defecto entidad |
| `FichaCostoDto_Validation_Margen_Excedido` | Validación límite 30% |
| `FichaCostoDto_Validation_Margen_Valido` | Validación aceptación |

---

## 🔧 CONFIGURACIÓN DEL ENTORNO

### URLs de Acceso Confirmadas
| Endpoint | URL | Estado |
|----------|-----|--------|
| Swagger UI 		| `http://localhost:5001/swagger` | ✅ Funcional |
| Swagger UI (alt) 	| `https://localhost:7048/swagger` | ✅ Funcional |
| API Base 			| `http://localhost:5001/api` | ⚠️ 404 (sin controllers - Fase 5) |

### Estructura de Carpetas Final Fase 2
```
PDL-FC-MVP/
├── src/
│   └── FichaCosto.Service/
│       ├── Data/
│       │   ├── DatabaseInitializer.cs      # Inicialización Dapper
│       │   └── Schema.sql                  # Esquema completo SQLite
│       ├── Models/
│       │   ├── DTOs/
│       │   │   ├── FichaCostoDto.cs        # Entrada + nested
│       │   │   ├── ResultadoCalculoDto.cs  # Salida cálculo
│       │   │   └── ResultadoValidacionDto.cs # Salida validación
│       │   ├── Entities/
│       │   │   ├── Cliente.cs
│       │   │   ├── FichaCosto.cs           # Con inicialización por defecto
│       │   │   ├── ManoObraDirecta.cs      # Con propiedades calculadas
│       │   │   ├── MateriaPrima.cs         # Con propiedades calculadas
│       │   │   └── Producto.cs
│       │   └── Enums/
│       │       ├── EstadoValidacion.cs     # Con NoDefinido=0
│       │       ├── TipoCosto.cs
│       │       └── UnidadMedida.cs
│       ├── Program.cs                      # Configurado con Serilog + Initializer
│       ├── appsettings.json                # ConnectionString SQLite
│       └── Properties/
│           └── launchSettings.json         # Puertos 5001/7048
├── tests/
│   └── FichaCosto.Service.Tests/
│       ├── DatabaseTests.cs                # 3 tests conexión/schema
│       ├── DtoTests.cs                     # 2 tests validación
│       └── EntityTests.cs                  # 2 tests lógica entidades (con alias)
└── Data/                                   # Creado en runtime
    └── fichacosto.db                       # BD SQLite con seed data
```

---

## 📊 ESTADO DE LA BASE DE DATOS

### Seed Data Insertado
| Tabla | Registros | Datos |
|-------|-----------|-------|
| `Clientes` 		| 1 | PyME Ejemplo S.A. (CUIT: 30123456789) |
| `Productos` 		| 1 | PROD-001 - Producto de Prueba |
| `MateriasPrimas` 	| 2 | Materia Prima A ($15.50), Materia Prima B ($25.00) |
| `ManoObraDirecta` | 1 | 2.5 horas, $850/hora, Ensamblaje manual |
| `FichasCosto` 	| 0 | (Se poblará en Fase 4 con cálculos) |

### Verificación Rápida
```powershell
# Conexión a BD desde PowerShell
cd "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\src\FichaCosto.Service\bin\Release\net9.0"
sqlite3 Data\fichacosto.db "SELECT * FROM vw_ProductosUltimoCosto;"
```

---

## 🎯 CONTEXTO PARA FASE 3: REPOSITORIOS

### Patrón a Implementar
| Repositorio | Interfaz | Operaciones MVP |
|-------------|----------|-----------------|
| `ClienteRepository` 		| `IClienteRepository` 	| CRUD básico |
| `ProductoRepository` 		| `IProductoRepository` | CRUD + listar por cliente + incluir MP/MO |
| `FichaRepository` 		| `IFichaRepository` 	| Crear ficha + historial por producto |

### Estrategia Dapper (sin EF Core)
- Queries SQL manuales en repositorios
- Mapeo `Query<T>` y `QueryAsync<T>`
- Transacciones con `BeginTransaction()`
- Sin migrations, schema fijo (mantenido en `Schema.sql`)

### Dependencias Ya Instaladas
| Paquete | Versión | Uso |
|---------|---------|-----|
| `Dapper` 							| 2.1.66 | Micro-ORM queries |
| `Microsoft.Data.Sqlite` 			| 9.0.0 | Driver SQLite |
| `SQLitePCLRaw.bundle_e_sqlite3` 	| 2.1.10 | Native bindings |

---

## 📝 DECISIONES TÉCNICAS DOCUMENTADAS

### 1. Dapper vs EF Core
**Decisión:** Dapper  
**Justificación:** Performance, control SQL exacto para normativas, menor overhead en MVP offline

### 2. Esquema Fijo vs Migrations
**Decisión:** Schema.sql + DatabaseInitializer  
**Justificación:** MVP con estructura estable, sin evolución compleja de BD prevista

### 3. SQLite vs SQL Server/PostgreSQL
**Decisión:** SQLite  
**Justificación:** Portabilidad, sin servidor requerido, archivo único fácil de respaldar

### 4. Puertos de Desarrollo
**Decisión:** Mantener 5001/7048 (VS 2022 defaults)  
**Nota:** Documentado para evitar confusión con puerto 5000 mencionado en especificaciones

---

## ⚠️ RIESGOS MITIGADOS

| Riesgo | Mitigación Aplicada |
|--------|---------------------|
| Conflicto nombres `FichaCosto` 	| Alias `using FichaCostoEntity` en tests |
| Enum sin valor 0 					| Agregado `NoDefinido = 0` + inicialización explícita |
| Zona horaria logs 				| Configuración Serilog con timestamps claros |
| Puerto dinámico VS 2022 			| Documentación explícita de URLs funcionales |

---

## 🔧 COMANDOS RÁPIDOS PARA CONTINUAR

```powershell
# Ubicarse en proyecto
cd "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP"

# Compilar y verificar
dotnet build --configuration Release --no-restore

# Ejecutar tests
dotnet test --verbosity normal

# Iniciar servicio (Swagger en http://localhost:5001/swagger)
cd src\FichaCosto.Service
dotnet run --launch-profile "http"

# Verificar BD
sqlite3 bin\Release\net9.0\Data\fichacosto.db ".tables"
```

---

## ✅ CHECKPOINT PARA INICIAR FASE 3

Antes de comenzar Fase 3 (Repositorios), verificar:

- [x] `dotnet build` genera 0 errores, 0 advertencias críticas
- [x] `dotnet test` pasa 7/7 tests
- [x] Servicio corre y Swagger accesible en `http://localhost:5001/swagger`
- [x] Base de datos `fichacosto.db` creada con 5 tablas + seed data
- [x] Logs funcionan correctamente en `Logs/log-YYYYMMdd.txt`
- [x] Entidades tienen valores por defecto correctos (`EstadoValidacion.Valido`)

**Si todos los checks pasan → Proceder a Fase 3**

---

## 📚 DOCUMENTACIÓN GENERADA

| Documento | Ubicación | Propósito |
|-----------|-----------|-----------|
| `RESUMEN-02.md` 				| `docs/` | Este documento - contexto para Fase 3 |
| `PROCEDIMIENTO-FASE-02.md` 	| `docs/` | Guía ultra-detallada de implementación |
| `Schema.sql` 					| `src/FichaCosto.Service/Data/` | Esquema SQLite versionado |
| `DatabaseInitializer.cs` 		| `src/FichaCosto.Service/Data/` | Inicialización programática |

---

**Fin del Resumen Fase 2**

*Documento generado para continuidad del desarrollo. Próximo entregable: Fase 3 - Repositorios con Dapper*


