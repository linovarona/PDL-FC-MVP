#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Desinstalación limpia de FichaCosto Service MVP
#>

param(
    [string]$InstallPath = "C:\Program Files\FichaCostoService",
    [string]$LogDir = "$env:TEMP\FichaCosto-Uninstall-Logs",
    [switch]$PreserveData,
    [switch]$Force,
    [switch]$ScheduleReboot  # Programar eliminación al reiniciar si está bloqueado
)

$ErrorActionPreference = "Stop"
$StartTime = Get-Date

New-Item -ItemType Directory -Path $LogDir -Force | Out-Null
$LogFile = "$LogDir\uninstall-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
Start-Transcript -Path $LogFile -Force

Write-Host "=== DESINSTALACION FICHA COSTO SERVICE ===" -ForegroundColor Cyan

function Invoke-SafeOperation {
    param([string]$Description, [scriptblock]$Operation, [switch]$ContinueOnError = $Force)
    Write-Host "`n[$Description]" -ForegroundColor Yellow
    try {
        & $Operation
        Write-Host "  ✓ $Description" -ForegroundColor Green
        return $true
    } catch {
        $msg = "  ✗ $Description fallo: $_"
        if ($ContinueOnError) { Write-Warning $msg; return $false } 
        else { Write-Error $msg; throw }
    }
}

# 1. DETENER SERVICIO Y PROCESOS
Invoke-SafeOperation "Deteniendo servicio y procesos" {
    # Detener servicio
    $service = Get-Service -Name "FichaCostoService" -ErrorAction SilentlyContinue
    if ($service -and $service.Status -eq 'Running') {
        Stop-Service -Name "FichaCostoService" -Force
        Start-Sleep -Seconds 5
    }
    
    # Matar proceso si persiste
    Get-Process | Where-Object { $_.ProcessName -like "*FichaCosto*" } | ForEach-Object {
        Write-Host "    Matando proceso: $($_.ProcessName) (PID: $($_.Id))" -ForegroundColor Gray
        $_ | Stop-Process -Force -ErrorAction SilentlyContinue
    }
    Start-Sleep -Seconds 3
    
    # Eliminar servicio
    & sc delete "FichaCostoService" 2>&1 | Out-Null
}

# 2. DESINSTALAR MSI
Invoke-SafeOperation "Desinstalando MSI" {
    $productCode = "{D8642967-675A-4281-8358-CB0E37465EB0}"
    $installed = Get-WmiObject -Class Win32_Product -Filter "IdentifyingNumber='$productCode'" -ErrorAction SilentlyContinue
    
    if ($installed) {
        $msiLog = "$LogDir\msi-uninstall.log"
        $proc = Start-Process -FilePath "msiexec.exe" -ArgumentList "/x", "$productCode", "/quiet", "/norestart", "/l*v", "`"$msiLog`"" -Wait -PassThru
        if ($proc.ExitCode -notin @(0, 3010)) { Write-Warning "MSI exit code: $($proc.ExitCode)" }
        Start-Sleep -Seconds 5
    }
}

# 3. LIMPIAR REGISTRO
Invoke-SafeOperation "Limpiando registro" {
    @(
        "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\{F52FBFC9-6AD6-4E8B-AED4-7E0C5279B78C}",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\{F52FBFC9-6AD6-4E8B-AED4-7E0C5279B78C}"
    ) | Where-Object { Test-Path $_ } | ForEach-Object { Remove-Item $_ -Recurse -Force }
    
    $cache = "C:\ProgramData\Package Cache\{F52FBFC9-6AD6-4E8B-AED4-7E0C5279B78C}"
    if (Test-Path $cache) { Remove-Item $cache -Recurse -Force }
}

# 4. ELIMINAR ARCHIVOS (CON MÚLTIPLES INTENTOS)
Invoke-SafeOperation "Eliminando archivos" {
    if (-not (Test-Path $InstallPath)) { return }
    
    if ($PreserveData) {
        # Preservar Data y Logs
        $exclude = @("Data", "Logs")
        Get-ChildItem $InstallPath | Where-Object { $_.Name -notin $exclude } | ForEach-Object {
            Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
        }
        return
    }
    
    # Método 1: Eliminar directo (reintentos)
    $maxAttempts = 3
    for ($i = 1; $i -le $maxAttempts; $i++) {
        try {
            Remove-Item -Path $InstallPath -Recurse -Force -ErrorAction Stop
            Write-Host "  ✓ Eliminado en intento $i" -ForegroundColor Green
            return
        } catch {
            Write-Host "  Intento $i fallido, esperando..." -ForegroundColor Yellow
            Start-Sleep -Seconds 3
            # Intentar liberar handles
            [GC]::Collect()
            [GC]::WaitForPendingFinalizers()
        }
    }
    
    # Método 2: Usar cmd rd (a veces funciona cuando PowerShell falla)
    Write-Host "  Intentando con cmd rd..." -ForegroundColor Yellow
    $result = cmd /c "rd /s /q `"$InstallPath`" 2>&1"
    if ($LASTEXITCODE -eq 0 -and -not (Test-Path $InstallPath)) {
        Write-Host "  ✓ Eliminado con cmd rd" -ForegroundColor Green
        return
    }
    
    # Método 3: Programar eliminación al reinicio
    if ($ScheduleReboot -or $Force) {
        Write-Host "  Programando eliminación al reinicio..." -ForegroundColor Yellow
        
        # Usar MoveFileEx con MOVEFILE_DELAY_UNTIL_REBOOT o registro
        $regPath = "HKLM:\SYSTEM\CurrentControlSet\Control\Session Manager"
        $regName = "PendingFileRenameOperations"
        
        # Renombrar carpeta a .old primero (más fácil de eliminar después)
        $oldPath = "$InstallPath.$(Get-Random).old"
        try {
            Rename-Item -Path $InstallPath -NewName $oldPath -Force
            Write-Host "  ✓ Renombrado a: $oldPath" -ForegroundColor Green
            
            # Agregar al registro para eliminar al reinicio
            $current = (Get-ItemProperty -Path $regPath -Name $regName -ErrorAction SilentlyContinue).$regName
            $newValue = if ($current) { $current + "`0" } else { "" }
            $newValue += "\??\$oldPath`0`0"
            Set-ItemProperty -Path $regPath -Name $regName -Value $newValue -Type MultiString
            
            Write-Host "  ⚠ Se requiere reinicio para completar la eliminación" -ForegroundColor Yellow
        } catch {
            # Si no se puede renombrar, intentar vaciar carpeta primero
            Write-Host "  Intentando vaciar contenido..." -ForegroundColor Yellow
            Get-ChildItem $InstallPath -Recurse | ForEach-Object {
                try { Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue } catch {}
            }
            throw "No se pudo eliminar completamente. Reinicio requerido."
        }
    } else {
        throw "Acceso denegado. Ejecutar con -ScheduleReboot para eliminar al reiniciar, o -Force para ignorar."
    }
}

# 5. FIREWALL
Invoke-SafeOperation "Eliminando firewall" {
    Get-NetFirewallRule -DisplayName "*FichaCosto*" -ErrorAction SilentlyContinue | Remove-NetFirewallRule
}

# RESUMEN
Write-Host "`n=== RESUMEN ===" -ForegroundColor Cyan
$checks = @(
    @{ Name="Servicio"; Test={ $null -eq (Get-Service FichaCostoService -EA SilentlyContinue) } },
    @{ Name="Carpeta"; Test={ -not (Test-Path $InstallPath) } },
    @{ Name="MSI"; Test={ $null -eq (Get-WmiObject Win32_Product -Filter "IdentifyingNumber='{D8642967-675A-4281-8358-CB0E37465EB0}'" -EA SilentlyContinue) } }
)

$allClean = $true
$checks | ForEach-Object {
    $result = & $_.Test
    $icon = if ($result) { "✓" } else { "✗" }
    $color = if ($result) { "Green" } else { "Red" }
    Write-Host "$icon $($_.Name)" -ForegroundColor $color
    if (-not $result) { $allClean = $false }
}

if (-not $allClean -and $ScheduleReboot) {
    Write-Host "`n⚠ REINICIO REQUERIDO para completar la desinstalación" -ForegroundColor Yellow
    $reboot = Read-Host "¿Reiniciar ahora? (S/N)"
    if ($reboot -eq 'S') { Restart-Computer -Force }
}

Stop-Transcript