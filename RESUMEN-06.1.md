
# RESUMEN-06.1.md
## Fase 6 Parte 1: Windows Service Funcional (MVP)
### Proyecto PDL-FC-MVP (FichaCosto Service)

**Fecha:** 29 Marzo 2026  
**Estado:** ✅ **COMPLETADO - Servicio Windows Instalado y Funcionando**  
**Versión:** v0.6.1  
**Próxima Etapa:** Fase 6 Parte 2 - Instalador MSI con WiX Toolset  
**Stack:** .NET 9.0 + Windows Service + Serilog + SQLite

---

## ✅ LOGROS COMPLETADOS

### 1. Configuración Servicio Windows
- [x] `Program.cs` configurado con `WindowsServiceHelpers.IsWindowsService()`
- [x] `UseWindowsService()` con ServiceName = "FichaCostoService"
- [x] `UseContentRoot(AppContext.BaseDirectory)` para rutas correctas en servicio
- [x] Serilog configurado con logs en archivo rotativo
- [x] Health Check endpoint: `/api/health` respondiendo OK

### 2. Publicación Self-Contained .NET 9.0
- [x] Runtime .NET 9.0 embebido (win-x64)
- [x] Publicación exitosa en modo NO single-file (para incluir e_sqlite3.dll)
- [x] Resolución de dependencia SQLite (e_sqlite3.dll presente)
- [x] Archivos de configuración: `appsettings.json`, `appsettings.Production.json`

### 3. Scripts de Gestión del Servicio (PowerShell)
| Script | Estado | Función |
|--------|--------|---------|
| `install-service.ps1` | ✅ Funcional | Instala servicio, configura permisos SID (S-1-5-20), inicia servicio |
| `uninstall-service.ps1` | ✅ Funcional | Elimina servicio y archivos |
| `check-service.ps1` | ✅ Funcional | Verifica estado, endpoint, logs |

### 4. Solución de Problemas Críticos Resueltos
- [x] **IConnectionFactory**: Registro agregado en DI container
- [x] **e_sqlite3.dll**: Publicación NO single-file para incluir librerías nativas
- [x] **Permisos NETWORK SERVICE**: Usando SID S-1-5-20 (universal, independiente de idioma)
- [x] **Creación de servicio**: Migrado de `sc.exe` a `New-Service` (nativo PowerShell)

---

## 📁 ESTRUCTURA ACTUAL DEL PROYECTO

```
PDL-FC-MVP/
├── src/
│   ├── FichaCosto.Service/
│   │   ├── Program.cs                    [MODIFICADO - Windows Service]
│   │   ├── appsettings.json              [CONFIGURADO - Serilog]
│   │   ├── appsettings.Production.json   [NUEVO - Producción]
│   │   └── FichaCosto.Service.csproj     [ACTUALIZADO - Serilog.Sinks.File]
│   └── FichaCosto.Installer/             [EXISTENTE - Vacío o con esqueleto]
├── scripts/
│   ├── install-service.ps1               [NUEVO - Funcional]
│   ├── uninstall-service.ps1             [NUEVO - Funcional]
│   └── check-service.ps1                 [NUEVO - Funcional]
├── publish/                              [GENERADO - Self-contained]
│   ├── FichaCosto.Service.exe            (~20MB)
│   ├── e_sqlite3.dll                     [REQUERIDO - SQLite nativo]
│   ├── appsettings*.json
│   └── ... (dependencias)
└── NuGetLocal/                           [OFFLINE]
    ├── packages/                         [Serilog.Sinks.File, etc.]
    └── runtimes/                         [Microsoft.NETCore.App.Runtime.win-x64.9.0.0, etc.]
```

## 🔧 CONFIGURACIÓN TÉCNICA CLAVE

### Program.cs - Cambios Esenciales
```csharp
// Windows Service detection
if (WindowsServiceHelpers.IsWindowsService())
{
    builder.Host.UseWindowsService(options => options.ServiceName = "FichaCostoService");
    builder.Host.UseContentRoot(AppContext.BaseDirectory);
}

// Serilog File Sink
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

// Inicialización SQLitePCL
Batteries_V2.Init(); // Si se usa single-file (actualmente no es el caso)
```

### Publish Profile (Comando Funcional)
```powershell
dotnet publish src\FichaCosto.Service\FichaCosto.Service.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    --source "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\NuGetLocal\runtimes" `
    --source "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\NuGetLocal\packages" `
    -o "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\publish"
```

**Nota:** Sin `-p:PublishSingleFile=true` para mantener e_sqlite3.dll separado y evitar errores de carga nativa.

### Servicio Windows - Configuración
- **Nombre:** FichaCostoService
- **Display Name:** FichaCosto Service MVP
- **Cuenta:** LocalSystem (implícito) / NETWORK SERVICE (permisos configurados)
- **Inicio:** Automático
- **Recuperación:** Reinicio en 1er y 2do fallo (5s y 10s)
- **Puerto:** 5000 (HTTP)
- **Logs:** `C:\Program Files\FichaCostoService\Logs\fichacosto-service-.log`

## 🔧 Distribución al Cliente

Lo que debes entregar al cliente (copiar a USB o comprimir):

```powershell
# Crear paquete de distribución
Compress-Archive -Path "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\publish\*" -DestinationPath "FichaCostoService-v0.6.1.zip"

# Contenido del ZIP (ejemplo):
# - FichaCosto.Service.exe    (ejecutable principal con runtime incluido)
# - e_sqlite3.dll             (SQLite nativo)
# - appsettings.json          (configuración)
# - appsettings.Production.json
# - Data/                     (esquema SQL si aplica)
# - Logs/                     (carpeta vacía inicial)
```

**Instalación en cliente:**
1. Descomprimir en `C:\Program Files\FichaCostoService\`
2. Ejecutar `install-service.ps1` como Admin
3. Listo - No requiere instalar nada más


## 📝 Nota para Documentación

En el futuro `MANUAL_USUARIO_MVP.md` (Fase 8), debe quedar claro:

| Requisito | Máquina Dev | Máquina Cliente |
|-----------|-------------|-----------------|
| .NET 9.0 SDK 			| ✅ Sí 					| ❌ No |
| .NET 9.0 Runtime 		| ✅ Sí (incluido en SDK) 	| ❌ No |
| Windows 10/11 64-bit 	| ✅ Sí 					| ✅ Sí |
| PowerShell 5.1+ 		| ✅ Sí 					| ✅ Sí (para scripts) |
| Puerto 5000 libre 	| ✅ Sí 					| ✅ Sí |


## 🧪 VERIFICACIÓN DE ESTADO ACTUAL

Comandos para verificar que todo funciona:

```powershell
# 1. Estado del servicio
Get-Service FichaCostoService
# Status: Running

# 2. Health Check
Invoke-RestMethod -Uri "http://localhost:5000/api/health"
# @{ status=OK; timestamp=...; version=v1.0.0-MVP }

# 3. Logs en tiempo real
Get-Content "C:\Program Files\FichaCostoService\Logs\fichacosto-service-.log" -Tail 20 -Wait

# 4. Swagger (si está habilitado en Production)
Start-Process "http://localhost:5000/swagger"
```

---

## 🚀 PRÓXIMOS PASOS (FASE 6 PARTE 2)

### Instalador MSI con WiX Toolset v4.0.5
- [ ] Configurar proyecto `FichaCosto.Installer` con WiX v4
- [ ] Crear `Package.wxs` con definición de componentes
- [ ] Harvest de archivos desde `publish/`
- [ ] Configurar servicio Windows en el MSI (sin scripts PowerShell)
- [ ] Generar `FichaCostoService-Setup-v1.0.0.msi`
- [ ] Prueba de instalación/desinstalación limpia

### Entregables Fase 6 Completa
- Scripts PowerShell (✅ Listo)
- Instalador MSI (⬜ Pendiente)
- Documentación de instalación (⬜ Pendiente)

---

## 💾 COMMIT RECOMENDADO

```powershell
# Ubicarse en raíz del proyecto
cd "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP"

# Verificar estado
git status
# Debe mostrar:
# - src/FichaCosto.Service/Program.cs (modified)
# - src/FichaCosto.Service/appsettings.Production.json (new)
# - src/FichaCosto.Service/FichaCosto.Service.csproj (modified)
# - scripts/install-service.ps1 (new)
# - scripts/uninstall-service.ps1 (new)
# - scripts/check-service.ps1 (new)

# Agregar cambios
git add src/FichaCosto.Service/
git add scripts/

# Commit
git commit -m "feat(fase-6.1): Windows Service funcional con .NET 9.0

- Configurar Program.cs para Windows Service con detección automática
- Agregar appsettings.Production.json para entorno producción
- Configurar Serilog con logging a archivo rotativo
- Resolver dependencia IConnectionFactory en DI
- Solucionar carga de e_sqlite3.dll (publicación NO single-file)
- Crear scripts PowerShell: install, uninstall, check
- Servicio instalado y verificado en localhost:5000
- Health Check respondiendo OK

Refs: RESUMEN-06.1.md"

# Tag intermedio (opcional)
git tag -a v0.6.1 -m "v0.6.1 - Fase 6 Parte 1: Windows Service funcional"
```

## ⚠️ NOTAS PARA CONTINUACIÓN (Fase 6.2)

### Dependencias Offline Requeridas para MSI
Descargar antes de continuar (máquina con internet):

```powershell
# WiX Toolset v4.0.5 (ya descargado presumiblemente)
# Verificar en: D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\Tools\WiX\

# HeatWave para VS 2022 (opcional, para edición visual de WiX)
# https://marketplace.visualstudio.com/items?itemName=FireGiant.FireGiantHeatWave
```

### Consideraciones WiX
- Usar `wix heat dir` para generar lista de archivos automáticamente desde `publish/`
- Componente de servicio: `<ServiceInstall>` y `<ServiceControl>`
- Definir `UpgradeCode` GUID fijo para futuras versiones
- Incluir `e_sqlite3.dll` como componente nativo

### Estado de Permisos
- SID S-1-5-20 (NETWORK SERVICE) funciona correctamente en español
- Permisos aplicados solo a carpeta `Logs`, no a todo `Program Files`
- Alternativa LocalSystem disponible si es necesario

---

**Estado:** Servicio Windows estable y funcional. Listo para empaquetar en MSI.  
**Bloqueante:** Ninguno. Proceder a Fase 6.2 cuando sea requerido.


## ⚠️ ACLARACIÓN: Requisitos en Host Cliente

### Máquina de Desarrollo (Tu PC)
- ✅ Requiere: **.NET 9.0 SDK** (instalado: dotnet-sdk-9.0.312-win-x64.exe)
- ✅ Para compilar, publicar y desarrollar

### Máquina Cliente (Donde se instala el servicio)
- ❌ **NO requiere** .NET 9.0 Runtime
- ❌ **NO requiere** .NET 9.0 SDK  
- ✅ Solo requiere: Windows 10/11 64-bit
- ✅ El runtime está **incluido** en la carpeta `publish/`

### Verificación Self-Contained
El EXE generado (~20MB) incluye:
- Runtime .NET 9.0 completo
- ASP.NET Core libraries
- SQLite nativo (e_sqlite3.dll)
- Aplicación y configuraciones

**Resultado:** El cliente ejecuta `FichaCosto.Service.exe` directamente sin instalaciones previas.
