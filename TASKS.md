## 📄 TASKS.md (Actualizado - Roadmap MVP)


# Plan de Desarrollo - Ficha de Costo MVP

## Sistema offline para automatización de fichas de costo según Resolución 148/2023 y 209/2024

---

## FASE 1: Configuración del proyecto ✅

**Objetivo:** Crear estructura inicial .NET 8.0 con configuración Windows Service.

**Entregables:**
- [x] Solución `FichaCosto.sln`
- [x] Proyecto `FichaCosto.Service` (Web API + Windows Service)
- [x] Proyecto `FichaCosto.Service.Tests` (xUnit)
- [x] Dependencias NuGet configuradas
- [x] Scripts de build/test/publish
- [x] `appsettings.json` configurado
- [x] `Program.cs` con Serilog + Windows Service

**Guía detallada:** Ver `docs/PROCEDIMIENTO-FASE-01.md`

---

## FASE 2: Modelos de datos y base de datos

**Objetivo:** Implementar entidades MVP, DTOs y esquema SQLite.

**Tareas:**

### 2.1 Enums
- [x] `TipoCosto` (MateriaPrima, ManoObra)
- [x] `UnidadMedida` (Kg, Unidad, Litro, etc.)

### 2.2 Entities (MVP - Simplificado)
- [x] `Cliente` (Id, Nombre, CUIT, Activo)
- [x] `Producto` (Id, ClienteId, Codigo, Nombre, UnidadMedida)
- [x] `MateriaPrima` (Id, ProductoId, Nombre, Cantidad, CostoUnitario)
- [x] `ManoObraDirecta` (Id, ProductoId, Horas, SalarioHora)
- [x] `FichaCosto` (Id, ProductoId, Fecha, CostosDirectos, MargenUtilidad, Valida)

### 2.3 DTOs MVP
- [x] `FichaCostoDto` (entrada cálculo)
- [x] `ResultadoCalculoDto` (salida cálculo)
- [x] `ResultadoValidacionDto` (validación 30%)

### 2.4 Base de datos
- [x] `FichaCostoContext` (SQLite + Dapper)
- [x] `Schema.sql` (tablas simplificadas MVP)
- [x] `DatabaseInitializer`

**Criterios de aceptación:**
- Compilación exitosa
- Base de datos SQLite creada automáticamente
- Migraciones no necesarias (esquema fijo)

---

## FASE 3: Repositorios (MVP)

**Objetivo:** Acceso a datos con Dapper (solo operaciones MVP).

**Tareas:**
- [ ] `IClienteRepository` (CRUD básico)
- [ ] `IProductoRepository` (CRUD + listar por cliente)
- [ ] `IFichaRepository` (crear + obtener historial)
- [ ] Implementaciones con Dapper
- [ ] Mapeos DTO-Entity básicos

**Nota:** Sin repositorios de CostosIndirectos/GastosGenerales para MVP (se harán en v1.1)

---

## FASE 4: Servicios de negocio (CORE)

**Objetivo:** Lógica de cálculo y validación según resoluciones.

**Tareas:**

### 4.1 CalculadoraCostoService
- [ ] `CalcularCostosDirectos()`:
  - Materias primas: Σ(Cantidad × CostoUnitario)
  - Mano obra: Horas × SalarioHora × (1 + CargasSociales/100)
- [ ] `CalcularPrecioVenta()`:
  - CostoTotal × (1 + MargenUtilidad/100)
- [ ] `ValidarMargen30()`:
  - Retorna error si > 30%
  - Advertencia si 25-30%

### 4.2 ValidadorFichaService
- [ ] Validar campos obligatorios
- [ ] Validar cantidades > 0
- [ ] Validar margen ≤ 30% (Res. 209/2024)

### 4.3 ExcelService (MVP)
- [ ] `ImportarProductoDesdeExcel()` (carga MP y MO)
- [ ] `ExportarFichaAExcel()` (genera archivo oficial)
- [ ] Validar formato de archivo

**Criterios de aceptación:**
- Cálculo correcto según Res. 148/2023
- Validación estricta del 30%
- Excel legible y formateado

---

## FASE 5: API REST Controllers (MVP)

**Objetivo:** Endpoints mínimos para funcionalidad MVP.

**Controllers:**

### CostosController
- [ ] `POST /api/costos/calcular` → `ResultadoCalculoDto`
- [ ] `POST /api/costos/validar` → `ResultadoValidacionDto`

### ClientesController (básico)
- [ ] `POST /api/clientes` (crear)
- [ ] `GET /api/clientes/{id}` (obtener)
- [ ] `GET /api/clientes` (listar)

### ProductosController (básico)
- [ ] `POST /api/productos` (crear)
- [ ] `GET /api/productos/{id}` (obtener con MP y MO)
- [ ] `GET /api/clientes/{id}/productos` (listar)

### ExcelController
- [ ] `POST /api/excel/importar` (file upload)
- [ ] `POST /api/excel/exportar` (genera archivo)

**Nota:** Sin autenticación para MVP (agregar en v1.1)

---

## FASE 6: Windows Service (MVP)

**Objetivo:** Configurar aplicación como servicio Windows.

**Tareas:**
- [ ] Configurar `Program.cs` para Windows Service
- [ ] Configurar logging a archivo en producción
- [ ] Crear instalador básico (script PowerShell)
- [ ] Probar instalación/desinstalación

**Postergado para v1.1:**
- Instalador MSI con WiX
- Gestión de servicio vía SCM

---

## FASE 7: Tests (MVP)

**Objetivo:** Cobertura de lógica crítica.

**Tests unitarios:**
- [ ] `CalculadoraCostoServiceTests` (5 tests):
  - Calcular costos directos correctamente
  - Calcular con margen 30% (válido)
  - Calcular con margen 35% (inválido)
  - Calcular precio de venta
  - Validar margen límite 30%
- [ ] `ExcelServiceTests` (2 tests):
  - Importar datos válidos
  - Exportar ficha correctamente

**Tests de integración:**
- [ ] `CostosControllerTests` (2 tests):
  - POST calcular retorna OK
  - POST validar detecta margen excesivo

**Criterio de aceptación:** 9/9 tests pasando

---

## FASE 8: Documentación y Release MVP

**Objetivo:** Documentación mínima para uso y despliegue.

**Tareas:**
- [ ] Actualizar `DOCUMENTACION_TECNICA.md` (arquitectura MVP)
- [ ] Crear `MANUAL_USUARIO_MVP.md`:
  - Cómo instalar el servicio
  - Cómo calcular una ficha
  - Cómo importar desde Excel
  - Cómo exportar resultados
- [ ] Crear `EJEMPLOS.md` con casos de uso
- [ ] Script de instalación `install-service.ps1`
- [ ] Release v1.0.0-MVP en GitHub

---

## Roadmap Post-MVP (v1.1+)

| Versión | Funcionalidades |
|---------|----------------|
| **v1.1** | Costos indirectos, Gastos generales, Instalador MSI |
| **v1.2** | Macros Excel VBA, Anexos oficiales, Historial completo |
| **v1.3** | Autenticación, Multi-usuario, Reportes avanzados |

---

## Convenciones de Desarrollo

### Commits
```
feat: Nueva funcionalidad
fix: Corrección de bug
test: Tests
docs: Documentación
refactor: Refactorización
```

### Estructura de ramas
- `main`: Código estable
- `develop`: Desarrollo activo
- `feature/fase-X`: Fases específicas

### Criterios de Done por Fase
1. Compilación exitosa (`dotnet build -c Release`)
2. Tests pasando (`dotnet test`)
3. Documentación actualizada
4. Commit en rama develop
```