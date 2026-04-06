#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Instalación de FichaCosto Service MVP
.DESCRIPTION
    Instala runtimes .NET y el servicio FichaCosto
#>

param(
    [string]$SourceDir = $PSScriptRoot,
    [string]$BundleName = "FichaCostoService-Bundle.exe",
    [string]$LogDir = "$env:TEMP\FichaCosto-Install-Logs"
)

$ErrorActionPreference = "Stop"
$StartTime = Get-Date

cd $SourceDir


# Crear directorio de logs de instalación
New-Item -ItemType Directory -Path $LogDir -Force | Out-Null
$MainLog = "$LogDir\install-main.log"
Start-Transcript -Path $MainLog -Force

Write-Host "=== FICHA COSTO SERVICE - INSTALACION ===" -ForegroundColor Cyan
Write-Host "Inicio: $StartTime" -ForegroundColor Gray
Write-Host "Origen: $SourceDir" -ForegroundColor Gray

# Verificar archivos necesarios
$requiredFiles = @(
    $BundleName,
    "post-install.ps1"
)

Write-Host "`n[1/4] Verificando archivos de instalacion..." -ForegroundColor Yellow
foreach ($file in $requiredFiles) {
    $filePath = Join-Path $SourceDir $file
    if (-not (Test-Path $filePath)) {
        throw "ERROR: Archivo requerido no encontrado: $file"
    }
    Write-Host "  ✓ $file encontrado" -ForegroundColor Green
}

# Ejecutar Bundle (Runtimes + MSI)
Write-Host "`n[2/4] Ejecutando instalador principal (Bundle)..." -ForegroundColor Yellow
Write-Host "    Esto instalara:" -ForegroundColor Gray
Write-Host "      - .NET 9.0 Runtime" -ForegroundColor Gray
Write-Host "      - ASP.NET Core 9.0 Runtime" -ForegroundColor Gray
Write-Host "      - Windows Desktop Runtime 9.0" -ForegroundColor Gray
Write-Host "      - FichaCosto Service MVP" -ForegroundColor Gray
Write-Host "`n    Tiempo estimado: 2-3 minutos..." -ForegroundColor Yellow

$bundlePath = Join-Path $SourceDir $BundleName
$bundleLog = "$LogDir\bundle-install.log"

try {
    $process = Start-Process -FilePath $bundlePath `
        -ArgumentList "/quiet", "/norestart", "/log `"$bundleLog`"" `
        -Wait `
        -PassThru
    
    if ($process.ExitCode -ne 0) {
        throw "ERROR: El Bundle fallo con codigo de salida: $($process.ExitCode)"
    }
    
    Write-Host "  ✓ Bundle instalado correctamente" -ForegroundColor Green
} catch {
    Write-Error "Falla en instalacion del Bundle: $_"
    Write-Host "`nRevisar log: $bundleLog" -ForegroundColor Yellow
    throw
}

# Verificar que el servicio fue creado
Write-Host "`n[3/4] Verificando instalacion del servicio..." -ForegroundColor Yellow
Start-Sleep -Seconds 3  # Esperar a que el servicio se registre

$service = Get-Service -Name "FichaCostoService" -ErrorAction SilentlyContinue
if (-not $service) {
    throw "ERROR: El servicio FichaCostoService no fue creado"
}

# Esperar a que el servicio inicie (puede tardar en primer arranque)
$retryCount = 0
$maxRetries = 30
while ($service.Status -ne 'Running' -and $retryCount -lt $maxRetries) {
    Write-Host "  Esperando inicio del servicio... ($retryCount/$maxRetries)" -ForegroundColor Gray
    Start-Sleep -Seconds 2
    $service.Refresh()
    $retryCount++
}

if ($service.Status -eq 'Running') {
    Write-Host "  ✓ Servicio instalado y ejecutandose" -ForegroundColor Green
    Write-Host "    Nombre: $($service.DisplayName)" -ForegroundColor Gray
    Write-Host "    Estado: $($service.Status)" -ForegroundColor Gray
    Write-Host "    Cuenta: LocalSystem" -ForegroundColor Gray
} else {
    Write-Warning "Servicio instalado pero no inicio automaticamente. Estado: $($service.Status)"
}

# Verificar estructura de directorios
Write-Host "`n[4/4] Verificando estructura de archivos..." -ForegroundColor Yellow
$installPath = "C:\Program Files\FichaCostoService"
$expectedDirs = @("Logs", "Data")

foreach ($dir in $expectedDirs) {
    $fullPath = Join-Path $installPath $dir
    if (Test-Path $fullPath) {
        Write-Host "  ✓ Directorio $dir existe" -ForegroundColor Green
    } else {
        Write-Warning "  ⚠ Directorio $dir no encontrado"
    }
}

# Resumen de tiempos
$EndTime = Get-Date
$Duration = $EndTime - $StartTime
Write-Host "`n=== INSTALACION COMPLETADA ===" -ForegroundColor Cyan
Write-Host "Duracion: $($Duration.Minutes) min $($Duration.Seconds) seg" -ForegroundColor Gray
Write-Host "Logs en: $LogDir" -ForegroundColor Gray

Stop-Transcript

# Ejecutar post-instalación automaticamente
$postInstall = Join-Path $SourceDir "post-install.ps1"
if (Test-Path $postInstall) {
    Write-Host "`nEjecutando post-instalacion..." -ForegroundColor Cyan
    & $postInstall -InstallPath $installPath -LogDir $LogDir
}