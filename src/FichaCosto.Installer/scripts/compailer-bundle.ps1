#Requires -RunAsAdministrator
param(
    [string]$Version = "0.6.2",
    [string]$LogDir = "$env:TEMP\FichaCosto-Install-Logs"
)

$ErrorActionPreference = "Stop"
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$logFile = "$LogDir\04-compiler-bundle-$timestamp.log"

Start-Transcript -Path $logFile -Force

Write-Host "=== 04. COMPILACIÓN BUNDLE ===" -ForegroundColor Cyan

. "$PSScriptRoot\config-extensions.ps1"
$exts = Test-WixExtensions

$basePath = "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP"
$installerPath = "$basePath\src\FichaCosto.Installer"
$wxsPath = "$installerPath\Bundle.wxs"
$outputBundle = "$installerPath\FichaCostoService-Bundle-v$Version.exe"
$msiPath = "$installerPath\FichaCostoService-Setup-v$Version.msi"

# Verificar prerequisitos
Write-Host "[1/2] Verificando prerequisitos..." -ForegroundColor Yellow
if (-not (Test-Path $msiPath)) {
    throw "MSI no encontrado. Ejecuta compailer-msi.ps1 primero."
}

# Compilar Bundle
Write-Host "`n[2/2] Compilando Bundle..." -ForegroundColor Yellow
$wixArgs = @(
    "build",
    "`"$wxsPath`"",
    "-o", "`"$outputBundle`"",
    "-arch", "x64",
    "-ext", "`"$($exts.Bal)`"",
    "-ext", "`"$($exts.Util)`""
)

& wix @wixArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n❌ Error compilando Bundle" -ForegroundColor Red
    Stop-Transcript
    exit 1
}

if (Test-Path $outputBundle) {
    $size = (Get-Item $outputBundle).Length / 1MB
    Write-Host "✅ Bundle generado: $outputBundle" -ForegroundColor Green
    Write-Host "   Tamaño: $([math]::Round($size,2)) MB" -ForegroundColor Green
} else {
    Write-Host "❌ Bundle no generado" -ForegroundColor Red
    exit 1
}

Write-Host "`n✅ COMPILACIÓN BUNDLE COMPLETADA" -ForegroundColor Green
Stop-Transcript