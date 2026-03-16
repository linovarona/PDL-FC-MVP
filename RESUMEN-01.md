
# RESUMEN-01: Fase 1 - Configuración del Entorno de Desarrollo
## Proyecto PDL-FC-MVP (FichaCosto Service)

**Fecha:** Marzo 2026  
**Estado:** ✅ Configuración base completada / ⚠️ Pendiente: Resolver nuget.config  
**Próxima Fase:** Fase 2 - Modelos de Datos y Base de Datos

---

## 📋 CONTEXTO DEL PROYECTO

### Objetivo MVP
Sistema de automatización de fichas de costo para PyMEs según Resoluciones 148/2023 y 209/2024, con arquitectura simplificada pero manteniendo Windows Service.

### Funcionalidades Críticas Identificadas
1. ✅ Cálculo de costos directos (materias primas + mano de obra)
2. ✅ Validación de márgenes de utilidad (máximo 30% según Res. 209/2024)
3. ✅ Generación de Excel de salida
4. ✅ Importación desde Excel

### Stack Tecnológico Confirmado
| Componente | Versión | Estado |
|------------|---------|--------|
| .NET SDK 		| 8.0 						| ✅ Instalado con VS 2022 |
| Visual Studio | 2022 Community/Pro/Ent 	| ✅ IDE principal |
| Base de datos | SQLite 3 					| ⚠️ Pendiente configurar |
| ORM 			| Dapper 2.1.66 			| ⚠️ Pendiente instalar |
| Excel 		| ClosedXML 0.104.2 		| ⚠️ Pendiente instalar |
| Logging 		| Serilog.AspNetCore 8.0.3 	| ⚠️ Pendiente instalar |
| Validación 	| FluentValidation 11.x 	| ⚠️ Pendiente instalar |
| Testing 		| xUnit + Moq 				| ⚠️ Pendiente instalar |

---

## 🗂️ ESTRUCTURA DE CARPETAS ESTABLECIDA

```
D:\PrjSC#\PDL\FichaCosto\
├── PDL-FC-MVP\                          ← Proyecto principal (NET 8.0)
│   ├── FichaCosto.sln                   ← Solución VS 2022
│   ├── nuget.config                     ← ⚠️ En corrección
│   ├── src\
│   │   └── FichaCosto.Service\          ← Web API + Windows Service
│   │       ├── FichaCosto.Service.csproj
│   │       ├── Program.cs               ← Configurado para Serilog + Windows Service
│   │       ├── appsettings.json         ← Configuración base
│   │       └── appsettings.Development.json
│   ├── tests\
│   │   └── FichaCosto.Service.Tests\    ← Proyecto xUnit
│   │       └── FichaCosto.Service.Tests.csproj
│   ├── NuGetLocal\                      ← Repositorio offline (en poblamiento)
│   │   ├── packages\                    ← 50+ paquetes .nupkg
│   │   └── descargar-paquetes.ps1       ← Script de descarga
│   ├── scripts\                           ← Scripts de automatización
│   │   ├── build.ps1                      ← MSBuild para VS 2022
│   │   ├── restore-offline.ps1            ← Restore sin internet
│   │   └── test.ps1                       ← Ejecutar tests
│   └── docs\                              ← Documentación
│       ├── RESUMEN-01.md                  ← Este documento
│       └── PROCEDIMIENTO-FASE-01.md       ← Guía detallada Fase 1
│
└── Tools\
    └── nuget.exe                          ← CLI NuGet standalone
```

---

## ✅ LOGROS COMPLETADOS (Fase 1)

### 1. Preparación del Entorno
- [x] Directorio de proyecto creado en `D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP`
- [x] Estructura de carpetas definida (src, tests, scripts, docs, NuGetLocal)
- [x] Git inicializado (opcional)

### 2. Solución y Proyectos
- [x] Solución `FichaCosto.sln` creada
- [x] Proyecto `FichaCosto.Service` (Web API .NET 8.0) creado
- [x] Proyecto `FichaCosto.Service.Tests` (xUnit) creado
- [x] Referencias entre proyectos configuradas

### 3. Configuración Base
- [x] `Program.cs` configurado con:
  - Serilog (logging a consola y archivo)
  - Windows Service hosting
  - Swagger/OpenAPI
  - CORS para cliente Excel
  - Creación automática de directorios (Data, Logs, Exportaciones, Plantillas)
- [x] `appsettings.json` con configuración de:
  - ConnectionStrings (SQLite)
  - ApiSettings (host, puerto)
  - Calculo (margen 30%, decimales)
  - Resolución (148/2023, 209/2024)

### 4. Repositorio NuGet Local (En Progreso)
- [x] Estructura `NuGetLocal\packages` creada
- [x] Script `descargar-paquetes.ps1` creado (50+ paquetes listados)
- [x] `nuget.exe` descargado en `D:\PrjSC#\PDL\FichaCosto\Tools\`
- [x] Paquetes descargados (en proceso o completado)

---

## ⚠️ PENDIENTES CRÍTICOS (Bloqueantes para Fase 2)

### Prioridad 1: Resolver Configuración NuGet
**Problema:** Error `Package source 'LocalPackages' must have at least one package pattern`

**Ubicación:** `D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\nuget.config`

**Solución aplicada:** Simplificar o eliminar `packageSourceMapping`, usar solo `packageSources`

**Próximo paso:**
```powershell
# 1. Limpiar config existente
Remove-Item "nuget.config" -ErrorAction SilentlyContinue

# 2. Crear config mínimo
@'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="LocalPackages" value="D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\NuGetLocal\packages" />
  </packageSources>
</configuration>
'@ | Out-File "nuget.config" -Encoding UTF8

# 3. Verificar
dotnet restore --source "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\NuGetLocal\packages"
```

### Prioridad 2: Instalar Paquetes en Proyectos
Una vez resuelto NuGet, instalar en este orden:

**Proyecto FichaCosto.Service:**
1. `Microsoft.EntityFrameworkCore.Sqlite` (8.0.2)
2. `Dapper` (2.1.66)
3. `FluentValidation.AspNetCore` (11.3.1)
4. `Serilog.AspNetCore` (8.0.3)
5. `Microsoft.Extensions.Hosting.WindowsServices` (8.0.0)
6. `Swashbuckle.AspNetCore` (6.9.0)
7. `ClosedXML` (0.104.2)

**Proyecto FichaCosto.Service.Tests:**
1. `Moq` (4.20.72)
2. `Microsoft.AspNetCore.Mvc.Testing` (8.0.2)

### Prioridad 3: Verificar Build
- [x] Compilar solución sin errores (`Ctrl+Shift+B`)
- [x] Verificar que `FichaCosto.Service.dll` se genera
- [x] Ejecutar con `F5` y verificar Swagger en `http://localhost:5000/swagger`

---

## 🎯 CONTEXTO PARA FASE 2: MODELOS DE DATOS

### Entidades a Implementar (MVP Simplificado)

| Entidad | Propósito | Relaciones |
|---------|-----------|------------|
| `Cliente` | Datos de la PyME | 1:N Productos |
| `Producto` | Bien/Servicio a costear | N:1 Cliente, 1:N MateriasPrimas, 1:1 ManoObra |
| `MateriaPrima` | Insumos directos | N:1 Producto |
| `ManoObraDirecta` | Costo de trabajo | 1:1 Producto |
| `FichaCosto` | Resultado del cálculo | N:1 Producto |

### DTOs Críticos
- `FichaCostoDto` - Entrada para cálculo
- `ResultadoCalculoDto` - Salida con costos y validación
- `ResultadoValidacionDto` - Estado de validación 30%

### Base de Datos
- **Motor:** SQLite 3
- **ORM:** Dapper (micro-ORM, no EF Core completo)
- **Schema:** SQL script en `Data/Schema.sql`
- **Inicializador:** `DatabaseInitializer.cs` con seed data

### Scripts SQL Pendientes
```sql
-- Tablas MVP:
-- 1. Clientes (Id, NombreEmpresa, CUIT, Activo)
-- 2. Productos (Id, ClienteId, Codigo, Nombre, UnidadMedida)
-- 3. MateriasPrimas (Id, ProductoId, Nombre, Cantidad, CostoUnitario)
-- 4. ManoObraDirecta (Id, ProductoId, Horas, SalarioHora, CargasSociales)
-- 5. FichasCosto (Id, ProductoId, Fecha, CostosDirectos, MargenUtilidad, Valida)
```

---

## 📚 DOCUMENTACIÓN GENERADA

| Documento | Ubicación | Propósito |
|-----------|-----------|-----------|
| `RESUMEN-01.md` | `docs/` | Este resumen - contexto para Fase 2 |
| `PROCEDIMIENTO-FASE-01.md` | `docs/` | Guía ultra-detallada de configuración |
| `DOCUMENTACION_TECNICA.md` | Raíz (entregado por usuario) | Especificación técnica completa |
| `TASKS.md` | Raíz (entregado por usuario) | Roadmap de fases |
| `README.md` | Raíz (entregado por usuario) | Documentación de usuario |

---

## 🔧 COMANDOS RÁPIDOS PARA CONTINUAR

```powershell
# Ubicarse en proyecto
cd "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP"

# Limpiar y restaurar (offline)
dotnet nuget locals all --clear
dotnet restore --source "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\NuGetLocal\packages"

# Compilar
dotnet build --configuration Release --no-restore

# Ejecutar
cd src\FichaCosto.Service
dotnet run

# Verificar
start http://localhost:5000/swagger
```

---

## 📝 NOTAS PARA EL DESARROLLADOR

### Decisiones de Arquitectura Tomadas
1. **Mantener Windows Service** (no simplificar a CLI) - requerimiento de despliegue
2. **SQLite + Dapper** (no EF Core completo) - más ligero, control total de SQL
3. **Minimal API vs Controladores** - pendiente decidir en Fase 5 (API)
4. **ClosedXML** (no EPPlus) - licencia más permisiva, compatible con MVP

### Riesgos Identificados
- ⚠️ NuGet offline puede tener dependencias transitivas faltantes
- ⚠️ Windows Service requiere permisos de administrador para instalar/debug
- ⚠️ SQLite en Windows Service requiere rutas absolutas (no relativa ~/Data)

### Recursos Externos Útiles (cuando haya internet)
- Dapper tutorial: https://dapper-tutorial.net/
- ClosedXML docs: https://docs.closedxml.io/
- Res. 148/2023 y 209/2024: Ver DOCUMENTACION_TECNICA.md sección 12

---

## ✅ CHECKPOINT PARA INICIAR FASE 2

Antes de comenzar Fase 2 (Modelos de Datos), verificar:

- [x] `dotnet restore` funciona sin errores (offline)
- [x] `dotnet build` genera 0 errores, 0 advertencias críticas
- [x] `F5` en VS 2022 inicia el servicio y muestra Swagger
- [x] Todos los paquetes NuGet están en `NuGetLocal\packages`
- [x] `nuget.config` permite restaurar sin internet

**Si todos los checks pasan → Proceder a Fase 2**

---

**Fin del Resumen Fase 1**

*Documento generado para continuidad del desarrollo. Actualizar si hay cambios en la configuración.*

