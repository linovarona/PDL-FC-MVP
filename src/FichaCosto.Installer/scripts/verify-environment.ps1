#Requires -RunAsAdministrator
param(
    [string]$LogDir = "$env:TEMP\FichaCosto-Install-Logs"
)

$ErrorActionPreference = "Stop"
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$fecha = Get-Date
$logFile = "$LogDir\01-verify-environment-$timestamp.log"

New-Item -ItemType Directory -Force -Path $LogDir | Out-Null
Start-Transcript -Path $logFile -Force

Write-Host "=== 01. VERIFICACION DE ENTORNO ===" -ForegroundColor Cyan
Write-Host "Fecha: $fecha" 
Write-Host "Log: $logFile`n"

# 1. Verificar WiX
Write-Host "[1/6] Verificando WiX Toolset..." -ForegroundColor Yellow
try {
    $wixVersion = wix --version 2>$null
    Write-Host " ✓ WiX instalado: $wixVersion" -ForegroundColor Green
} catch {
    Write-Host "  WiX no encontrado. Instalar: dotnet tool install wix --version 4.0.6 -g" -ForegroundColor Red
    Stop-Transcript
    exit 1
}

# 2. Verificar Extensiones Offline
Write-Host "`n[2/6] Verificando extensiones WiX offline..." -ForegroundColor Yellow
. "$PSScriptRoot\config-extensions.ps1"
try {
    $exts = Test-WixExtensions
    Write-Host " ✓ Todas las extensiones disponibles offline" -ForegroundColor Green
} catch {
    Write-Host "   $($_.Exception.Message)" -ForegroundColor Red
    Stop-Transcript
    exit 1
}

# 3. Verificar .NET 9 SDK
Write-Host "`n[3/6] Verificando .NET 9 SDK..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version 2>$null
if ($dotnetVersion -match "^9\.") {
    Write-Host "  ✓ .NET SDK 9.x detectado: $dotnetVersion" -ForegroundColor Green
} else {
    Write-Host "  âš ï¸  .NET SDK 9 no detectado. VersiÃ³n actual: $dotnetVersion" -ForegroundColor Yellow
}

# 4. Verificar estructura de carpetas
Write-Host "`n[4/6] Verificando estructura de proyecto..." -ForegroundColor Yellow
$basePath = "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP"
$paths = @(
    "$basePath\src\FichaCosto.Service",
    "$basePath\src\FichaCosto.Installer",
    "$basePath\src\FichaCosto.Service\Data\Schema.sql",
    "$basePath\src\FichaCosto.Service\Data\SeedData.sql"
)

foreach ($path in $paths) {
    if (Test-Path $path) {
        Write-Host "  ✓ $(Split-Path $path -Leaf)" -ForegroundColor Green
    } else {
        Write-Host "  âŒ No encontrado: $path" -ForegroundColor Red
    }
}

# 5. Verificar archivos de runtime (para bundle)
Write-Host "`n[5/6] Verificando runtimes .NET para Bundle..." -ForegroundColor Yellow
$runtimes = @(
    "dotnet-runtime-9.0.3-win-x64.exe",
    "aspnetcore-runtime-9.0.3-win-x64.exe",
    "windowsdesktop-runtime-9.0.3-win-x64.exe"
)
$installerPath = "$basePath\src\FichaCosto.Installer"
foreach ($rt in $runtimes) {
    $rtPath = Join-Path $installerPath $rt
    if (Test-Path $rtPath) {
        $size = (Get-Item $rtPath).Length / 1MB
        Write-Host "  ✓ $rt ($([math]::Round($size,1))) MB" -ForegroundColor Green
    } else {
        Write-Host "  âš ï¸  No encontrado: $rt (se descargarÃ¡ automÃ¡ticamente si hay internet)" -ForegroundColor Yellow
    }
}

# 6. Verificar espacio en disco
Write-Host "`n[6/6] Verificando espacio en disco..." -ForegroundColor Yellow
$drive = Get-Item $basePath
$freeSpaceGB = [math]::Round(($drive.PSDrive.Free / 1GB), 2)
if ($freeSpaceGB -gt 2) {
    Write-Host "  ✓ Espacio disponible: $freeSpaceGB GB" -ForegroundColor Green
} else {
    Write-Host "  âŒ Espacio insuficiente: $freeSpaceGB GB (se requieren 2GB)" -ForegroundColor Red
}

Write-Host "`n=== VERIFICACION COMPLETADA ===" -ForegroundColor Green
Stop-Transcript