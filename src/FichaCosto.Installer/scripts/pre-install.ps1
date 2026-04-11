#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Pre-instalación: Verifica requisitos y prepara el entorno
.DESCRIPTION
    Realiza comprobaciones previas a la instalación del FichaCosto Service
#>

param(
    [string]$InstallPath = "C:\Program Files\FichaCostoService",
    [string]$LogPath = "$env:TEMP\FichaCosto-Install-Logs\05-pre-install-*.log"
)

$ErrorActionPreference = "Stop"
Start-Transcript -Path $LogPath -Force

Write-Host "=== FICHA COSTO SERVICE - PRE-INSTALACION ===" -ForegroundColor Cyan
Write-Host "Fecha: $(Get-Date)" -ForegroundColor Gray

# 1. Verificar Windows 10/11 64-bit
Write-Host "`n[1/5] Verificando sistema operativo..." -ForegroundColor Yellow
$os = Get-WmiObject -Class Win32_OperatingSystem
if ($os.OSArchitecture -ne "64 bits") {
    throw "ERROR: Se requiere Windows 64-bits. Detectado: $($os.OSArchitecture)"
}
$build = [System.Environment]::OSVersion.Version.Build
if ($build -lt 10240) {
    throw "ERROR: Se requiere Windows 10 (build 10240+) o Windows 11. Detectado build: $build"
}
Write-Host "  ✓ Windows 64-bit detectado (Build: $build)" -ForegroundColor Green

# 2. Verificar privilegios de Administrador
Write-Host "`n[2/5] Verificando privilegios de administrador..." -ForegroundColor Yellow
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "ERROR: Se requieren privilegios de Administrador. Ejecute como Administrator."
}
Write-Host "  ✓ Ejecutando con privilegios de Administrador" -ForegroundColor Green

# 3. Verificar espacio en disco
Write-Host "`n[3/5] Verificando espacio en disco..." -ForegroundColor Yellow
$systemDrive = Get-WmiObject -Class Win32_LogicalDisk -Filter "DeviceID='C:'"
$freeSpaceGB = [math]::Round($systemDrive.FreeSpace / 1GB, 2)
if ($freeSpaceGB -lt 1) {
    throw "ERROR: Espacio insuficiente. Requerido: 1 GB, Disponible: $freeSpaceGB GB"
}
Write-Host "  ✓ Espacio disponible: $freeSpaceGB GB" -ForegroundColor Green

# 4. Verificar puerto 5000 disponible
Write-Host "`n[4/5] Verificando disponibilidad del puerto 5000..." -ForegroundColor Yellow
$portInUse = Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue
if ($portInUse) {
    Write-Warning "Puerto 5000 en uso por: $($portInUse.OwningProcess)"
    $process = Get-Process -Id $portInUse.OwningProcess -ErrorAction SilentlyContinue
    Write-Warning "Proceso: $($process.ProcessName)"
    Write-Host "  ⚠ El servicio podría no iniciar si el puerto está ocupado" -ForegroundColor Yellow
} else {
    Write-Host "  ✓ Puerto 5000 disponible" -ForegroundColor Green
}

# 5. Verificar versión de .NET si ya está instalado
Write-Host "`n[5/5] Verificando instalaciones previas de .NET..." -ForegroundColor Yellow
try {
    $dotnetVersion = dotnet --version 2>$null
    if ($dotnetVersion) {
        Write-Host "  ℹ .NET SDK detectado: $dotnetVersion" -ForegroundColor Cyan
    }
    
    $runtimes = dotnet --list-runtimes 2>$null | Where-Object { $_ -match "9\.0\.3" }
    if ($runtimes) {
        Write-Host "  ✓ .NET 9.0.3 Runtime ya instalado:" -ForegroundColor Green
        $runtimes | ForEach-Object { Write-Host "    - $_" -ForegroundColor Gray }
    } else {
        Write-Host "  ℹ .NET 9.0.3 no detectado. Se instalará con el Bundle." -ForegroundColor Yellow
    }
} catch {
    Write-Host "  ℹ .NET no detectado en PATH. Se instalará con el Bundle." -ForegroundColor Yellow
}

# 6. Crear directorio de respaldo si existe instalación previa
$backupDir = "$env:TEMP\FichaCosto-Backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
if (Test-Path $InstallPath) {
    Write-Host "`n[!] Instalacion previa detectada en $InstallPath" -ForegroundColor Yellow
    Write-Host "    Creando respaldo en: $backupDir" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
    Copy-Item -Path "$InstallPath\*" -Destination $backupDir -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  ✓ Respaldo creado" -ForegroundColor Green
}

# 7. Detener servicio si existe (para reinstalación limpia)
Write-Host "`n[6/6] Verificando servicio existente..." -ForegroundColor Yellow
$service = Get-Service -Name "FichaCostoService" -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "  ℹ Servicio existente detectado. Deteniendo..." -ForegroundColor Yellow
    Stop-Service -Name "FichaCostoService" -Force -ErrorAction SilentlyContinue
    Write-Host "  ✓ Servicio detenido" -ForegroundColor Green
} else {
    Write-Host "  ✓ No hay servicio previo" -ForegroundColor Green
}

Write-Host "`n=== PRE-INSTALACION COMPLETADA ===" -ForegroundColor Cyan
Write-Host "Sistema listo para instalacion." -ForegroundColor Green
Write-Host "Log guardado en: $LogPath" -ForegroundColor Gray

Stop-Transcript