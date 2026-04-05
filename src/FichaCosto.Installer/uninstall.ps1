#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Desinstalación limpia de FichaCosto Service MVP
.DESCRIPTION
    Elimina completamente el servicio, archivos, registro de Windows y configuraciones
    Revierte todas las operaciones realizadas por install.ps1 y post-install.ps1
#>

param(
    [string]$InstallPath = "C:\Program Files\FichaCostoService",
    [string]$LogDir = "$env:TEMP\FichaCosto-Uninstall-Logs",
    [switch]$PreserveData,  # Mantener base de datos y logs históricos
    [switch]$Force          # Forzar eliminación incluso si hay errores
)

$ErrorActionPreference = "Stop"
$StartTime = Get-Date

# Crear directorio de logs
New-Item -ItemType Directory -Path $LogDir -Force | Out-Null
$LogFile = "$LogDir\uninstall-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
Start-Transcript -Path $LogFile -Force

Write-Host "=== FICHA COSTO SERVICE - DESINSTALACION LIMPIA ===" -ForegroundColor Cyan
Write-Host "Inicio: $StartTime" -ForegroundColor Gray
Write-Host "Modo PreserveData: $PreserveData" -ForegroundColor Gray
Write-Host "Modo Force: $Force" -ForegroundColor Gray

# Función helper para manejar errores
function Invoke-SafeOperation {
    param(
        [string]$Description,
        [scriptblock]$Operation,
        [switch]$ContinueOnError = $Force
    )
    
    Write-Host "`n[$Description]" -ForegroundColor Yellow
    try {
        & $Operation
        Write-Host "  ✓ $Description completado" -ForegroundColor Green
        return $true
    } catch {
        if ($ContinueOnError) {
            Write-Warning "  ⚠ $Description fallo (ignorado por -Force): $_"
            return $false
        } else {
            Write-Error "  ✗ $Description fallo: $_"
            throw
        }
    }
}

# 1. DETENER Y ELIMINAR SERVICIO WINDOWS
Invoke-SafeOperation "Deteniendo servicio FichaCostoService" {
    $service = Get-Service -Name "FichaCostoService" -ErrorAction SilentlyContinue
    
    if ($service) {
        if ($service.Status -eq 'Running') {
            Write-Host "    Deteniendo servicio..." -ForegroundColor Gray
            Stop-Service -Name "FichaCostoService" -Force -ErrorAction Stop
            Start-Sleep -Seconds 3
            
            # Verificar que realmente se detuvo
            $service.Refresh()
            if ($service.Status -ne 'Stopped') {
                # Intentar kill del proceso si persiste
                $proc = Get-Process -Name "FichaCosto.Service" -ErrorAction SilentlyContinue
                if ($proc) {
                    Write-Host "    Forzando terminacion de proceso..." -ForegroundColor Gray
                    $proc | Stop-Process -Force
                    Start-Sleep -Seconds 2
                }
            }
        }
        
        Write-Host "    Eliminando servicio..." -ForegroundColor Gray
        # Usar sc.exe para eliminación limpia
        $result = & sc.exe delete "FichaCostoService" 2>&1
        if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne 1060) {  # 1060 = servicio no existe
            throw "sc.exe fallo con codigo $LASTEXITCODE : $result"
        }
        
        # Verificar eliminación
        Start-Sleep -Seconds 2
        $verifyService = Get-Service -Name "FichaCostoService" -ErrorAction SilentlyContinue
        if ($verifyService) {
            throw "El servicio sigue existiendo despues de eliminarlo"
        }
    } else {
        Write-Host "    Servicio no encontrado (ya fue eliminado)" -ForegroundColor Gray
    }
}

# 2. DESINSTALAR MSI (ProductCode conocido o búsqueda)
Invoke-SafeOperation "Desinstalando MSI" {
    $productCode = "{D8642967-675A-4281-8358-CB0E37465EB0}"  # Del Package.wxs
    
    # Verificar si está instalado
    $installedProduct = Get-WmiObject -Class Win32_Product -Filter "IdentifyingNumber='$productCode'" -ErrorAction SilentlyContinue
    
    if ($installedProduct) {
        Write-Host "    Desinstalando ProductCode $productCode..." -ForegroundColor Gray
        $msiLog = "$LogDir\msi-uninstall.log"
        
        $process = Start-Process -FilePath "msiexec.exe" `
            -ArgumentList "/x", "$productCode", "/quiet", "/norestart", "/l*v", "`"$msiLog`"" `
            -Wait `
            -PassThru
        
        if ($process.ExitCode -ne 0 -and $process.ExitCode -ne 3010) {  # 3010 = success, reboot required
            throw "MSI desinstalacion fallo con codigo $($process.ExitCode)"
        }
        
        # Esperar a que Windows termine
        Start-Sleep -Seconds 5
    } else {
        Write-Host "    MSI no encontrado en registro de Windows" -ForegroundColor Gray
    }
}

# 3. ELIMINAR REGISTRO DEL BUNDLE (BURN)
Invoke-SafeOperation "Eliminando registro del Bundle" {
    # El Bundle deja registro en Package Cache y Uninstall registry
    $bundleProviderKey = "{F52FBFC9-6AD6-4E8B-AED4-7E0C5279B78C}"  # Del bundle log
    
    # Buscar en registry
    $bundleRegPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$bundleProviderKey"
    $bundleRegPathWow = "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\$bundleProviderKey"
    
    if (Test-Path $bundleRegPath) {
        Remove-Item -Path $bundleRegPath -Recurse -Force
        Write-Host "    Eliminado registro Bundle (x64)" -ForegroundColor Gray
    }
    
    if (Test-Path $bundleRegPathWow) {
        Remove-Item -Path $bundleRegPathWow -Recurse -Force
        Write-Host "    Eliminado registro Bundle (WOW64)" -ForegroundColor Gray
    }
    
    # Limpiar Package Cache
    $packageCache = "C:\ProgramData\Package Cache\$bundleProviderKey"
    if (Test-Path $packageCache) {
        Remove-Item -Path $packageCache -Recurse -Force
        Write-Host "    Eliminado Package Cache" -ForegroundColor Gray
    }
    
    # Buscar y eliminar cualquier registro de FichaCosto en Add/Remove Programs por si acaso
    $fichaCostoApps = Get-ChildItem "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall" | 
        Get-ItemProperty | 
        Where-Object { $_.DisplayName -like "*FichaCosto*" }
    
    foreach ($app in $fichaCostoApps) {
        Remove-Item -Path $app.PSPath -Recurse -Force
        Write-Host "    Eliminado registro residual: $($app.DisplayName)" -ForegroundColor Gray
    }
}

# 4. ELIMINAR ARCHIVOS Y CARPETAS
Invoke-SafeOperation "Eliminando archivos de instalacion" {
    if (Test-Path $InstallPath) {
        if ($PreserveData) {
            Write-Host "    Modo PreserveData: Manteniendo Data y Logs" -ForegroundColor Yellow
            
            # Eliminar solo archivos del programa, preservar Data y Logs
            $itemsToDelete = Get-ChildItem -Path $InstallPath | Where-Object { 
                $_.Name -notin @("Data", "Logs") 
            }
            
            foreach ($item in $itemsToDelete) {
                Remove-Item -Path $item.FullName -Recurse -Force
                Write-Host "    Eliminado: $($item.Name)" -ForegroundColor Gray
            }
            
            # Crear nota indicando que es una desinstalación parcial
            "Desinstalacion parcial realizada el $(Get-Date)`nServicio eliminado pero datos preservados." | 
                Out-File -FilePath "$InstallPath\README_UNINSTALL.txt" -Force
            
        } else {
            Write-Host "    Eliminando carpeta completa: $InstallPath" -ForegroundColor Gray
            
            # Intentar eliminar, si falla (archivos bloqueados), forzar con takeown/icacls
            try {
                Remove-Item -Path $InstallPath -Recurse -Force
            } catch {
                Write-Host "    Intentando forzar permisos..." -ForegroundColor Yellow
                takeown /F "$InstallPath" /R /D Y 2>&1 | Out-Null
                icacls "$InstallPath" /grant Administrators:F /T 2>&1 | Out-Null
                Remove-Item -Path $InstallPath -Recurse -Force -ErrorAction Stop
            }
        }
    } else {
        Write-Host "    Carpeta de instalacion no existe" -ForegroundColor Gray
    }
    
    # Limpiar archivos temporales de instalación
    $tempItems = @(
        "$env:TEMP\FichaCosto-Install-Logs",
        "$env:TEMP\FichaCosto-PostInstall",
        "$env:TEMP\FichaCosto-PreInstall"
    )
    
    foreach ($tempItem in $tempItems) {
        if (Test-Path $tempItem) {
            Remove-Item -Path $tempItem -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host "    Eliminado temporal: $tempItem" -ForegroundColor Gray
        }
    }
}

# 5. ELIMINAR REGLAS DE FIREWALL
Invoke-SafeOperation "Eliminando reglas de firewall" {
    $ruleName = "FichaCosto Service - Puerto 5000"
    
    $existingRule = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
    
    if ($existingRule) {
        Remove-NetFirewallRule -DisplayName $ruleName -ErrorAction Stop
        Write-Host "    Eliminada regla: $ruleName" -ForegroundColor Gray
    } else {
        Write-Host "    Regla de firewall no encontrada" -ForegroundColor Gray
    }
    
    # Buscar otras reglas relacionadas por si acaso
    $otherRules = Get-NetFirewallRule | Where-Object { 
        $_.DisplayName -like "*FichaCosto*" -or $_.DisplayName -like "*fichacosto*"
    }
    
    foreach ($rule in $otherRules) {
        Remove-NetFirewallRule -Name $rule.Name -ErrorAction SilentlyContinue
        Write-Host "    Eliminada regla adicional: $($rule.DisplayName)" -ForegroundColor Gray
    }
}

# 6. LIMPIAR LOGS DE EVENTOS (Opcional)
Invoke-SafeOperation "Limpiando registros de eventos" {
    # Limpiar logs de la aplicación relacionados con nuestro servicio
    try {
        $logs = Get-WinEvent -FilterHashtable @{
            LogName = 'Application'
            ProviderName = 'FichaCostoService'
        } -ErrorAction SilentlyContinue
        
        if ($logs) {
            Write-Host "    Preservados $(($logs | Measure-Object).Count) eventos en Application Log" -ForegroundColor Gray
            # Nota: No eliminamos eventos individuales, solo limpiamos si es necesario
        }
        
        # Limpiar logs de instalación MSI antiguos
        $msiLogs = Get-ChildItem "$env:TEMP" -Filter "MSI*.log" -ErrorAction SilentlyContinue | 
            Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-7) }
        
        if ($msiLogs) {
            $msiLogs | Remove-Item -Force -ErrorAction SilentlyContinue
            Write-Host "    Limpiados $($msiLogs.Count) logs MSI antiguos" -ForegroundColor Gray
        }
    } catch {
        Write-Host "    No se requieren acciones en event logs" -ForegroundColor Gray
    }
}

# 7. VERIFICACION FINAL
Write-Host "`n[7/7] Verificando desinstalacion completa..." -ForegroundColor Yellow

$checks = @{
    "Servicio eliminado" = $null -eq (Get-Service -Name "FichaCostoService" -ErrorAction SilentlyContinue)
    "Carpeta de programa" = -not (Test-Path $InstallPath)
    "Registro MSI" = $null -eq (Get-WmiObject -Class Win32_Product -Filter "IdentifyingNumber='$productCode'" -ErrorAction SilentlyContinue)
    "Reglas firewall" = $null -eq (Get-NetFirewallRule -DisplayName "*FichaCosto*" -ErrorAction SilentlyContinue)
}

Write-Host "`n=== RESULTADO DE VERIFICACION ===" -ForegroundColor Cyan
$allClean = $true
foreach ($check in $checks.GetEnumerator()) {
    $status = if ($check.Value) { "✓ LIMPIO" } else { "✗ PRESENTE" }
    $color = if ($check.Value) { "Green" } else { "Red" }
    Write-Host "$($check.Key): " -NoNewline
    Write-Host $status -ForegroundColor $color
    if (-not $check.Value) { $allClean = $false }
}

# 8. RESUMEN Y BACKUP DE DATOS (Si PreserveData)
$EndTime = Get-Date
$Duration = $EndTime - $StartTime

Write-Host "`n=== DESINSTALACION COMPLETADA ===" -ForegroundColor Cyan
Write-Host "Duracion: $($Duration.Minutes) min $($Duration.Seconds) seg" -ForegroundColor Gray
Write-Host "Log guardado en: $LogFile" -ForegroundColor Gray

if ($PreserveData -and (Test-Path "$InstallPath\Data")) {
    Write-Host "`n⚠ ATENCION: Datos preservados en:" -ForegroundColor Yellow
    Write-Host "   $InstallPath\Data" -ForegroundColor Yellow
    Write-Host "   $InstallPath\Logs" -ForegroundColor Yellow
    Write-Host "   Eliminar manualmente si ya no se necesitan." -ForegroundColor Yellow
}

if ($allClean) {
    Write-Host "`n✓ Sistema limpio. FichaCosto Service ha sido completamente eliminado." -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n⚠ Algunos componentes no pudieron eliminarse. Revisar log: $LogFile" -ForegroundColor Yellow
    if (-not $Force) {
        Write-Host "   Ejecutar con -Force para ignorar errores." -ForegroundColor Gray
    }
    exit 1
}

Stop-Transcript