#Requires -RunAsAdministrator
param(
    [string]$Version = "0.6.2",
    [string]$LogDir = "$env:TEMP\FichaCosto-Install-Logs"
)

$ErrorActionPreference = "Stop"
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$logFile = "$LogDir\03-compiler-msi-$timestamp.log"

Start-Transcript -Path $logFile -Force

Write-Host "=== 03. COMPILACIÓN MSI (x64) ===" -ForegroundColor Cyan

# Cargar configuración de extensiones
. "$PSScriptRoot\config-extensions.ps1"
$exts = Test-WixExtensions

$basePath = "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP"
$installerPath = "$basePath\src\FichaCosto.Installer"
$publishPath = "$basePath\src\FichaCosto.Service\bin\Release\net9.0\win-x64\publish"
$wxsPath = "$installerPath\Package.wxs"
$outputMsi = "$installerPath\FichaCostoService-Setup-v$Version.msi"

# Verificar prerequisitos
Write-Host "[1/3] Verificando prerequisitos..." -ForegroundColor Yellow
if (-not (Test-Path $publishPath)) {
    throw "No se encontró la carpeta publish. Ejecuta publisher.ps1 primero."
}
if (-not (Test-Path $wxsPath)) {
    throw "No se encontró Package.wxs en $installerPath"
}

# Verificar que SQLite.Interop.dll existe (crítico para el seed)
$interopPath = "$publishPath\runtimes\win-x64\native\SQLite.Interop.dll"
if (-not (Test-Path $interopPath)) {
    Write-Host "⚠️  Advertencia: SQLite.Interop.dll no encontrado en runtimes/win-x64/native/" -ForegroundColor Yellow
    Write-Host "   El seed de base de datos podría fallar." -ForegroundColor Yellow
} else {
    Write-Host "✅ SQLite.Interop.dll encontrado" -ForegroundColor Green
    # Copiar a raíz para facilitar el acceso desde PowerShell
    Copy-Item $interopPath $publishPath -Force
}

# Compilar MSI
Write-Host "`n[2/3] Compilando MSI con extensiones offline..." -ForegroundColor Yellow
Write-Host "Usando extensiones locales..." -ForegroundColor Gray

$wixArgs = @(
    "build",
    "`"$wxsPath`"",
    "-o", "`"$outputMsi`"",
    "-arch", "x64",
    "-d", "PublishDir=$publishPath",
    "-ext", "`"$($exts.Util)`"",
    "-ext", "`"$($exts.Firewall)`""
)

Write-Host "Comando: wix $($wixArgs -join ' ')" -ForegroundColor DarkGray

& wix @wixArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Error compilando MSI" -ForegroundColor Red
    Stop-Transcript
    exit 1
}

# Verificar salida
Write-Host "`n[3/3] Verificando MSI generado..." -ForegroundColor Yellow
if (Test-Path $outputMsi) {
    $size = (Get-Item $outputMsi).Length / 1MB
    Write-Host "✅ MSI generado: $outputMsi" -ForegroundColor Green
    Write-Host "   Tamaño: $([math]::Round($size,2)) MB" -ForegroundColor Green
    
    # Verificar con WiX
    Write-Host "`nValidando MSI..." -ForegroundColor Gray
    wix msi validate $outputMsi
} else {
    Write-Host "❌ MSI no encontrado en $outputMsi" -ForegroundColor Red
    Stop-Transcript
    exit 1
}

Write-Host "`n✅ COMPILACIÓN MSI COMPLETADA" -ForegroundColor Green
Stop-Transcript