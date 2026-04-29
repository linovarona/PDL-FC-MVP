# RESUMEN-06.2.md
## Fase 6 Parte 2: Instalador WiX Bundle + MSI (MVP)
### Proyecto PDL-FC-MVP (FichaCosto Service)

**Fecha:** 05 Abril 2026  
**Estado:** ✅ **COMPLETADO - Instalador Bundle Funcional y Servicio Operativo**  
**Versión:** v0.6.2  
**Próxima Etapa:** Fase 7 - Documentación Técnica y Manual de Usuario  
**Stack:** .NET 9.0 + WiX Toolset v4 + Windows Service + PowerShell

---

## ✅ LOGROS COMPLETADOS

### 1. Instalador WiX Bundle (Bootstrapper)
- [x] `Bundle.wxs` configurado con WiX Toolset v4
- [x] Instalación silenciosa de runtimes .NET 9.0 (dotnet, ASP.NET, Desktop)
- [x] Detección automática de runtimes instalados (RegistrySearch)
- [x] Instalación secuenciada: Runtimes → MSI → Servicio
- [x] Tema `rtfLargeLicense` con versión visible
- [x] Ejecutable único: `FichaCostoService-Bundle.exe` (~105MB)

### 2. Instalador MSI (Package.wxs)
- [x] Estructura de directorios: `INSTALLFOLDER`, `LOGSFOLDER`, `DATAFOLDER`
- [x] Componente de Servicio Windows con `ServiceInstall` y `ServiceControl`
- [x] Cuenta `LocalSystem` con inicio automático
- [x] Harvest de archivos desde `publish/` (excluyendo ejecutable principal)
- [x] Componente de servicio con `File` + `ServiceInstall` unificados
- [x] `MajorUpgrade` configurado para versiones futuras

### 3. Scripts de Instalación Automatizada (PowerShell)
| Script | Estado | Función |
|--------|--------|---------|
| `pre-install.ps1` | ✅ Funcional | Verificaciones pre-instalación (SO, permisos, puertos, espacio) |
| `install.ps1` | ✅ Funcional | Ejecuta Bundle, verifica servicio, lanza post-install |
| `post-install.ps1` | ✅ Funcional | Permisos ACL, inicializa SQLite, firewall, verificación HTTP |

### 4. Solución de Problemas Críticos Resueltos
- [x] **Error 1618 (MSI anidados)**: Migrado de Custom Actions a Bundle (Burn)
- [x] **Error 1920 (Servicio no inicia)**: Componente `ServiceInstall` con `File` integrado
- [x] **Error ACL (Identidad no resuelta)**: Uso de `icacls` con SID `S-1-5-18` en lugar de .NET ACL
- [x] **Servicio no inicia automáticamente**: Retry explícito en `install.ps1` post-MSI
- [x] **SQLite no inicializada**: Creación de `fichacosto.db` vacío + permisos en `post-install.ps1`

---

## 📁 ESTRUCTURA FINAL DEL PROYECTO

```
PDL-FC-MVP/
├── src/
│   ├── FichaCosto.Service/
│   │   ├── Program.cs                    [Windows Service configurado]
│   │   ├── appsettings.json              [Serilog configurado]
│   │   ├── appsettings.Production.json   [Configuración producción]
│   │   └── FichaCosto.Service.csproj     [.NET 9.0, Self-contained]
│   └── FichaCosto.Installer/
│       ├── Bundle.wxs                    [NUEVO - Bootstrapper runtimes]
│       ├── Package.wxs                   [NUEVO - MSI componentes]
│       ├── FichaCostoService-Bundle.exe  [GENERADO - Instalador final]
│       ├── license.rtf                   [Vacío - Requerido por Bundle]
│       ├── pre-install.ps1               [NUEVO - Verificaciones]
│       ├── install.ps1                   [NUEVO - Instalación completa]
│       └── post-install.ps1              [NUEVO - Configuración post]
├── scripts/                              [DEPRECATED - Reemplazados por installer]
│   ├── install-service.ps1
│   ├── uninstall-service.ps1
│   └── check-service.ps1
├── publish/                              [GENERADO - Self-contained]
│   ├── FichaCosto.Service.exe
│   ├── e_sqlite3.dll
│   ├── appsettings*.json
│   └── Data/Schema.sql
└── NuGetLocal/                           [Offline packages]
```

---

## 🔧 CONFIGURACIÓN TÉCNICA CLAVE

### Bundle.wxs - Estructura del Bootstrapper
```xml
<Bundle Name="FichaCosto Service MVP Bundle" Version="0.6.1" ...>
  <BootstrapperApplication>
    <bal:WixStandardBootstrapperApplication Theme="rtfLargeLicense" .../>
  </BootstrapperApplication>
  
  <Chain>
    <ExePackage Id="NetRuntime" ... DetectCondition="NetRuntimeInstalled"/>
    <ExePackage Id="AspNetRuntime" ... DetectCondition="AspNetRuntimeInstalled"/>
    <ExePackage Id="DesktopRuntime" ... DetectCondition="DesktopRuntimeInstalled"/>
    <MsiPackage Id="FichaCostoService" SourceFile="FichaCostoService-Setup-v0.6.1.msi"/>
  </Chain>
  
  <util:RegistrySearch Id="NetRuntimeInstalled" ... Variable="NetRuntimeInstalled"/>
</Bundle>
```

### Package.wxs - Componente de Servicio (Patrón Correcto)
```xml
<Component Id="Comp_Service" Directory="INSTALLFOLDER" Guid="...">
  <!-- File DEBE estar en el mismo componente que ServiceInstall -->
  <File Id="File_ServiceExe" Source="..\..\publish\FichaCostoService.exe" 
        KeyPath="yes" Vital="yes" />
  
  <ServiceInstall Id="ServiceInstaller"
                  Name="FichaCostoService"
                  DisplayName="FichaCosto Service MVP"
                  Start="auto"
                  Type="ownProcess"
                  Account="LocalSystem"
                  Arguments="--environment Production"
                  Vital="yes" />
                  
  <ServiceControl Id="ServiceControl"
                  Name="FichaCostoService"
                  Start="install"
                  Stop="both"
                  Remove="uninstall"
                  Wait="yes" />
</Component>
```

### post-install.ps1 - Permisos con icacls (SID)
```powershell
# Método robusto para sistemas en español
icacls $logsPath /grant "*S-1-5-18:(OI)(CI)F"    # SYSTEM
icacls $logsPath /grant "Administradores:(OI)(CI)F"  # Administradores
icacls $dataPath /grant "*S-1-5-18:(OI)(CI)F"    # Para SQLite
```

---

## 📦 Distribución al Cliente Final

### Entregables para Despliegue

| Archivo | Tamaño | Descripción |
|---------|--------|-------------|
| `FichaCostoService-Bundle.exe` | ~105 MB | Instalador único (runtimes + app) |
| `install.ps1` | 3 KB | Script de instalación automatizada |
| `post-install.ps1` | 8 KB | Configuración post-instalación |
| `pre-install.ps1` | 4 KB | Verificaciones pre-instalación |

### Proceso de Instalación en Cliente

```powershell
# 1. Ejecutar como Administrador
.\install.ps1

# 2. Esperar 2-3 minutos (instalación silenciosa de runtimes)

# 3. Verificar instalación
Get-Service FichaCostoService  # Status: Running
Start-Process "http://localhost:5000/swagger"
```

### Requisitos del Cliente (Mínimos)
| Requisito | Especificación |
|-----------|---------------|
| Sistema Operativo | Windows 10/11 64-bit (Build 10240+) |
| Privilegios 		| Administrador local |
| Puerto TCP 		| 5000 (libre) |
| Espacio en disco 	| 500 MB libres |
| .NET Runtime		| ❌ NO requerido (incluido en Bundle) |
| PowerShell 		| 5.1+ (para scripts opcionales) |

---

## 🧪 VERIFICACIÓN DE INSTALACIÓN

### Comandos de Validación Post-Instalación

```powershell
# 1. Verificar servicio está ejecutándose
Get-Service FichaCostoService | Select-Object Name, Status, StartType

# 2. Verificar endpoint HTTP
Invoke-RestMethod -Uri "http://localhost:5000/api/health"
# @{ status = "OK"; timestamp = "..."; version = "v0.6.2" }

# 3. Verificar estructura de archivos
Get-ChildItem "C:\Program Files\FichaCostoService" -Recurse

# 4. Verificar logs escribiendo
Get-Content "C:\Program Files\FichaCostoService\Logs\install-test.log"

# 5. Verificar base de datos inicializada
Test-Path "C:\Program Files\FichaCostoService\Data\fichacosto.db"

# 6. Verificar reglas de firewall
Get-NetFirewallRule -DisplayName "FichaCosto Service*"
```

### Diagnóstico de Problemas

| Síntoma | Causa Probable | Solución |
|---------|---------------|----------|
| Servicio no inicia 	| Puerto 5000 ocupado | `netstat -ano \| findstr :5000` |
| Logs vacíos 			| Permisos ACL incorrectos | Ejecutar `post-install.ps1` nuevamente |
| SQLite error 			| BD no inicializada | Verificar `fichacosto.db` existe |
| Error 1618 			| Instalación previa incompleta | Reiniciar Windows, reintentar |
| Bundle no ejecuta 	| Falta `license.rtf` | Crear archivo vacío en misma carpeta |

---

## 🚀 PRÓXIMOS PASOS (FASE 7)

### Documentación Técnica y Manual de Usuario
- [ ] Crear `MANUAL_USUARIO_MVP.md` (guía para cliente no técnico)
- [ ] Crear `MANUAL_TECNICO_MVP.md` (guía para administradores IT)
- [ ] Documentar troubleshooting y FAQs
- [ ] Crear guía de desinstalación limpia
- [ ] Documentar proceso de upgrade de versión

### Entregables Fase 7
| Documento | Audiencia | Contenido |
|-----------|-----------|-----------|
| `MANUAL_USUARIO_MVP.md` 	| Cliente PyME | Instalación paso a paso, uso básico, FAQs |
| `MANUAL_TECNICO_MVP.md` 	| Admin IT | Arquitectura, configuración avanzada, backup |
| `CHANGELOG.md` 			| Desarrolladores | Historial de versiones, breaking changes |
| `README.md` 				| GitHub | Overview, quickstart, badges |

### Consideraciones para Fase 7
- Screenshots de Swagger UI para manual de usuario
- Diagrama de arquitectura (servicio → SQLite → logs)
- Procedimiento de backup de `Data\fichacosto.db`
- Guía de cambio de puerto (5000 → otro)

---

## 💾 COMMIT RECOMENDADO

```powershell
# Ubicarse en raíz del proyecto
cd "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP"

# Verificar estado
git status
# Debe mostrar:
# - src/FichaCosto.Installer/Bundle.wxs (new)
# - src/FichaCosto.Installer/Package.wxs (new)
# - src/FichaCosto.Installer/license.rtf (new)
# - src/FichaCosto.Installer/pre-install.ps1 (new)
# - src/FichaCosto.Installer/install.ps1 (new)
# - src/FichaCosto.Installer/post-install.ps1 (new)
# - src/FichaCosto.Installer/FichaCostoService-Bundle.exe (new, o en .gitignore)
# - scripts/ (modified o deleted si se movieron)

# Agregar cambios (excluir EXE grande del repo)
git add src/FichaCosto.Installer/*.wxs
git add src/FichaCosto.Installer/*.ps1
git add src/FichaCosto.Installer/license.rtf
git add .gitignore  # Si se agregó FichaCostoService-Bundle.exe

# Commit
git commit -m "feat(fase-6.2): Instalador WiX Bundle + MSI completo

- Crear Bundle.wxs con Bootstrapper para runtimes .NET 9.0
- Crear Package.wxs con componente ServiceInstall integrado
- Resolver Error 1618 (MSI anidados) usando Burn
- Resolver Error 1920 con patrón File+ServiceInstall unificado
- Crear scripts PowerShell: pre-install, install, post-install
- Implementar permisos ACL robustos con icacls + SID S-1-5-18
- Inicializar SQLite y configurar firewall en post-install
- Servicio operativo en http://localhost:5000/swagger
- Instalador único de ~105MB con runtimes embebidos

Refs: RESUMEN-06.1.md, RESUMEN-06.2.md
BREAKING CHANGE: Reemplaza scripts de instalación manual por Bundle"

# Tag de versión completa Fase 6
git tag -a v0.6.2 -m "v0.6.2 - Fase 6 Completa: Windows Service + Instalador WiX Bundle

Fase 6.1: Servicio Windows funcional con .NET 9.0
Fase 6.2: Instalador WiX Bundle con runtimes embebidos
- Instalación automatizada en 3 pasos (pre/install/post)
- Servicio LocalSystem con inicio automático
- SQLite + Logs configurados automáticamente
- Zero-config para cliente final"

# Push (si hay remote configurado)
git push origin main --tags
```

---

## 📋 DIFERENCIAS Fase 6.1 vs 6.2

| Aspecto | Fase 6.1 (Scripts) | Fase 6.2 (WiX Bundle) |
|---------|-------------------|----------------------|
| Instalación 		| Manual (xcopy + PS) 		| Automatizada (EXE único) |
| Runtimes .NET 	| Requería pre-instalación 	| Embebidos en Bundle |
| Proceso 			| 5-10 minutos técnico 		| 2-3 minutos click-next |
| Rollback 			| Manual 					| Automático (Burn) |
| Detección estado 	| Scripts check-service 	| RegistrySearch en Bundle |
| Permisos 			| Scripts PowerShell 		| icacls en post-install |
| Desinstalación 	| Manual					| Panel de Control → Desinstalar |

---

## ⚠️ NOTAS TÉCNICAS PARA MANTENIMIENTO

### Actualización de Versión (Checklist)
1. Modificar `Version` en `Bundle.wxs`
2. Modificar `Version` en `Package.wxs`
3. Actualizar `UpgradeCode` si es major version
4. Recompilar Bundle: `wix build Bundle.wxs -ext ...`
5. Actualizar `install.ps1` si cambia nombre de archivo
6. Tag git nuevo: `git tag -a v0.6.3`

### Troubleshooting WiX
```powershell
# Verificar extensiones instaladas
wix extension list

# Compilar verbose
wix build Bundle.wxs -ext WixToolset.BootstrapperApplications.wixext -v

# Log detallado del Bundle
FichaCostoService-Bundle.exe /log "C:\Temp\bundle-debug.log"
```

### Límites Conocidos (MVP)
- Puerto 5000 hardcoded (configurable en appsettings.json post-instalación)
- SQLite sin encriptación (MVP)
- Logs sin rotación por tamaño (solo por fecha)
- Sin autenticación en API (MVP abierto)

---

**Estado:** Fase 6 COMPLETADA. Instalador profesional listo para distribución a cliente.  
**Bloqueante:** Ninguno. Proceder a Fase 7 (Documentación).  
**Nota:** El Bundle.exe (~105MB) no debe versionarse en Git (agregar a `.gitignore`), solo los fuentes `.wxs` y scripts `.ps1`.
```
