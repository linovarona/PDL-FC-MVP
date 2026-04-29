Kimi: ## 📊 RESUMEN DE SESIÓN: Fase 6.5 - Instalador WiX (Offline)

**Fecha:** 11 Abril 2026  
**Estado:** 🟡 **En Progreso** - Publicación funcionando, MSI en depuración  
**Versión Target:** v0.6.2 (x64)

---

## ✅ LOGROS ALCANZADOS

### 1. Entorno de Build Estabilizado
| Componente | Estado | Detalle |
|------------|--------|---------|
| **WiX CLI** | ✅ | v4.0.6 funcionando offline |
| **Extensiones WiX** | ✅ | Bal, Util, UI, Firewall descargadas y referenciadas vía rutas absolutas |
| **.NET 9 SDK** | ✅ | v9.0.312 detectado y operativo |
| **Publicación x64** | ✅ | Servicio compilando correctamente para `win-x64` |
| **Dependencias SQLite** | ✅ | Identificado uso de `Microsoft.Data.Sqlite` (EF Core), no `System.Data.SQLite` |

### 2. Scripts Administrativos Secuenciales (Pipeline de Instalación)
Implementado flujo de trabajo paso a paso con logging individual:

```
verify-environment.ps1  →  publisher.ps1  →  compailer-msi.ps1  →  
compailer-bundle.ps1    →  pre-install.ps1  →  install.ps1  →  
post-install.ps1  →  [seed-repair.ps1 si es necesario]
```

**Cada script genera:**  
- Log en `C:\Users\Yo\AppData\Local\Temp\FichaCosto-Install-Logs\`  
- Verificación de prerequisitos del paso anterior  
- Validación de salida antes de continuar

### 3. Configuración Extensiones Offline
**Archivo creado:** `config-extensions.ps1`  
**Función:** `Test-WixExtensions` verifica disponibilidad de:
- `WixToolset.Util.wixext.dll`
- `WixToolset.Firewall.wixext.dll`
- `WixToolset.Bal.wixext.dll`
- `WixToolset.UI.wixext.dll`

**Ruta:** `C:\Users\Yo\.wix\extensions\v4\`

### 4. Corrección de Arquitectura
- Cambiado de `ProgramFilesFolder` (x86) a `ProgramFiles64Folder` (x64 nativo)
- Publicación targeteando `win-x64` con `--self-contained false`
- Inclusión de `e_sqlite3.dll` (native library) en el MSI

---

## 🔴 PROBLEMAS PENDIENTES

### Bloqueo Actual: FirewallRule en WiX v4
**Error:** `WIX0005: The File/Component/ServiceInstall element contains an unexpected child element 'FirewallRule'`

**Intentos fallidos:**
1. ❌ `FirewallRule` como hijo directo de `Component`
2. ❌ `FirewallRule` dentro de `File`
3. ❌ `FirewallRule` dentro de `ServiceInstall`

**Causa:** Cambio de esquema en WiX v4 que requiere sintaxis específica no documentada claramente para el uso combinado con `ServiceInstall`.

**Alternativa validada:**  
Mover la creación de regla de firewall al script `post-install.ps1` usando:
```powershell
New-NetFirewallRule -DisplayName "FichaCosto Service" -Direction Inbound -LocalPort 5000 -Protocol TCP -Action Allow
```

---

## 📋 ESTRATEGIA DEFINIDA PARA PRÓXIMA SESIÓN

### Opción A: Scripts Administrativos (Mantenimiento/Técnicos)
- **Seed de BD:** Script `seed-repair.ps1` usando `Microsoft.Data.Sqlite` (ya desarrollado)
- **Permisos Logs:** Script `repair-permissions.ps1` con `icacls` o `Set-Acl`
- **Firewall:** Script `add-firewall-rule.ps1` con `New-NetFirewallRule`

### Opción B: Bundle de Usuario Final (Automático)
- **MSI:** Solo instala archivos y servicio (sin lógica compleja de WiX)
- **Seed de BD:** Realizado por el servicio al primer arranque (implementar en C#)
- **Post-Install:** Script PowerShell ejecutado automáticamente después del MSI que configura:
  - Permisos de carpetas
  - Regla de firewall
  - Verificación de BD poblada

---

## 🎯 TAREAS PENDIENTES (Fase 6.5 - Continuación)

### Prioridad 1: Finalizar MSI Funcional
- [ ] Simplificar `Package.wxs` eliminando `fire:FirewallRule` (mover a post-install)
- [ ] Validar `PermissionEx` dentro de `CreateFolder` (último error pendiente)
- [ ] Generar GUIDs reales para componentes especiales
- [ ] Compilar MSI x64 exitosamente

### Prioridad 2: Integración Post-Install
- [ ] Desarrollar `post-install.ps1` completo con:
  - Creación de regla firewall vía PowerShell
  - Verificación de permisos de Logs
  - Trigger de seed de BD si está vacía
  - Tests de conectividad HTTP

### Prioridad 3: Bundle Final
- [ ] Integrar MSI + Runtimes en Bundle
- [ ] Test de instalación end-to-end en máquina limpia
- [ ] Validar que el servicio arranca automáticamente post-instalación

---

## 📁 ARCHIVOS MODIFICADOS EN ESTA SESIÓN
- `src/FichaCosto.Installer/scripts/config-extensions.ps1` *(nuevo)*
- `src/FichaCosto.Installer/scripts/verify-environment.ps1` *(nuevo)*
- `src/FichaCosto.Installer/scripts/publisher.ps1` *(corregido para Microsoft.Data.Sqlite)*
- `src/FichaCosto.Installer/scripts/compailer-msi.ps1` *(corregido para rutas offline)*
- `src/FichaCosto.Installer/scripts/compailer-bundle.ps1` *(corregido)*
- `src/FichaCosto.Installer/scripts/post-install.ps1` *(pendiente de agregar firewall)*
- `src/FichaCosto.Installer/scripts/seed-repair.ps1` *(nuevo, para reparación manual)*
- `src/FichaCosto.Installer/Package.wxs` *(múltiples iteraciones, pendiente simplificación)*

---

## 💾 RECOMENDACIÓN PARA CONTINUAR (Siguiente Sesión)

**Estrategia "Divide y Vencerás":**

1. **Simplificar `Package.wxs`** eliminando todo lo que cause errores de validación:
   - Quitar `FirewallRule` de WiX (mover a PowerShell)
   - Quitar `PermissionEx` si sigue fallando (manejar vía post-install)
   - Dejar solo: archivos, servicio, y creación de carpetas básicas

2. **Fortalecer `post-install.ps1`** como el "orquestador" de configuración:
   - Todo lo que WiX no pueda hacer bien (firewall, permisos complejos), lo hace PowerShell
   - Más fácil de debuggear y mantener que XML de WiX

3. **Validar flujo completo** una vez que el MSI compile limpio.

**Próximo paso inmediato:** Corregir `Package.wxs` con la estrategia de simplificación y generar primer MSI exitoso.

---

**Estado de memoria guardado para sesión siguiente.**
