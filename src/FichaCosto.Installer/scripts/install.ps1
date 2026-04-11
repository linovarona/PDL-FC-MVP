#Requires -RunAsAdministrator
param(
    [string]$SourceDir = $PSScriptRoot,
    [string]$BundleName = "FichaCostoService-Bundle.exe",
    [string]$LogDir = "$env:TEMP\06-install-*.log"
)

$ErrorActionPreference = "Stop"
$StartTime = Get-Date

New-Item -ItemType Directory -Path $LogDir -Force | Out-Null
$MainLog = "$LogDir\install-main.log"
Start-Transcript -Path $MainLog -Force

Write-Host "=== INSTALacion FICHA COSTO SERVICE ===" -ForegroundColor Cyan

# 1. VERIFICAR ARCHIVOS
Write-Host "`n[1/4] Verificando archivos..." -ForegroundColor Yellow
$bundlePath = Join-Path $SourceDir $BundleName
if (-not (Test-Path $bundlePath)) { throw "No se encuentra $BundleName" }
Write-Host "  ✓ Bundle encontrado" -ForegroundColor Green

# 2. DESINSTALAR PREVIO SI EXISTE (LIMPIEZA)
Write-Host "`n[2/4] Verificando instalaciones previas..." -ForegroundColor Yellow
$existingService = Get-Service FichaCostoService -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "  ! Servicio existente detectado. Deteniendo..." -ForegroundColor Yellow
    Stop-Service FichaCostoService -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 3
    & sc delete FichaCostoService 2>&1 | Out-Null
    Write-Host "  ✓ Servicio previo eliminado" -ForegroundColor Green
}

# Verificar si hay registro previo del MSI
$productCode = "{D8642967-675A-4281-8358-CB0E37465EB0}"
$installed = Get-WmiObject -Class Win32_Product -Filter "IdentifyingNumber='$productCode'" -ErrorAction SilentlyContinue
if ($installed) {
    Write-Host "  ! MSI previo detectado. Desinstalando..." -ForegroundColor Yellow
    Start-Process msiexec -ArgumentList "/x $productCode /quiet /norestart" -Wait
    Start-Sleep -Seconds 5
}

# 3. EJECUTAR BUNDLE
Write-Host "`n[3/4] Ejecutando Bundle (esto puede tardar 2-3 minutos)..." -ForegroundColor Yellow
$bundleLog = "$LogDir\bundle-install.log"

$proc = Start-Process -FilePath $bundlePath -ArgumentList "/log `"$bundleLog`"" -Wait -PassThru

Write-Host "  Bundle finalizado con código: $($proc.ExitCode)" -ForegroundColor $(if($proc.ExitCode -eq 0){'Green'}else{'Red'})

# Dar tiempo a que Windows registre todo
Write-Host "  Esperando registro de componentes (15 segundos)..." -ForegroundColor Gray
Start-Sleep -Seconds 15

# 4. VERIFICAR INSTALACIÓN CON RETRIES
Write-Host "`n[4/4] Verificando instalación..." -ForegroundColor Yellow

$service = $null
$retries = 0
$maxRetries = 10

while (-not $service -and $retries -lt $maxRetries) {
    $service = Get-Service FichaCostoService -ErrorAction SilentlyContinue
    if (-not $service) {
        $retries++
        Write-Host "  Esperando registro del servicio... ($retries/$maxRetries)" -ForegroundColor Gray
        Start-Sleep -Seconds 3
    }
}

if (-not $service) {
    Write-Error "ERROR: El servicio no fue creado después de $($maxRetries * 3) segundos"
    Write-Host "`nDiagnosticando..." -ForegroundColor Yellow
    
    # Revisar log del Bundle
    if (Test-Path $bundleLog) {
        $lastErrors = Get-Content $bundleLog -Tail 20 | Select-String "error|fail|return 3"
        Write-Host "Errores recientes en Bundle:" -ForegroundColor Red
        $lastErrors | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
    }
    
    # Verificar si el MSI está instalado aunque el servicio no exista
    $msiInstalled = Get-WmiObject -Class Win32_Product -Filter "IdentifyingNumber='$productCode'" -ErrorAction SilentlyContinue
    if ($msiInstalled) {
        Write-Host "`nEl MSI está instalado pero el servicio no existe." -ForegroundColor Yellow
        Write-Host "Ejecutando reparación..." -ForegroundColor Yellow
        Start-Process msiexec -ArgumentList "/fa $productCode /quiet /l*v `"$LogDir\msi-repair.log`"" -Wait
    }
    
    throw "Instalación fallida. Revisar logs en $LogDir"
}

Write-Host "  ✓ Servicio registrado" -ForegroundColor Green

# Iniciar servicio si no está corriendo
if ($service.Status -ne 'Running') {
    Write-Host "  Iniciando servicio..." -ForegroundColor Yellow
    try {
        Start-Service FichaCostoService
        $service.WaitForStatus('Running', '00:00:30')
        Write-Host "  ✓ Servicio iniciado" -ForegroundColor Green
    } catch {
        Write-Warning "No se pudo iniciar automáticamente. Iniciar manualmente con: Start-Service FichaCostoService"
    }
}

# Verificar HTTP
Start-Sleep -Seconds 3
try {
    $resp = Invoke-WebRequest http://localhost:5000/api/health -UseBasicParsing -TimeoutSec 5
    if ($resp.StatusCode -eq 200) {
        Write-Host "  ✓ Endpoint HTTP respondiendo" -ForegroundColor Green
    }
} catch {
    Write-Warning "HTTP no responde aún (puede tardar más en iniciar)"
}

Write-Host "`n=== INSTALACION COMPLETADA ===" -ForegroundColor Cyan
Write-Host "Logs: $LogDir" -ForegroundColor Gray
Write-Host "Servicio: $(Get-Service FichaCostoService | Select-Object Status, StartType | Out-String)" -ForegroundColor Gray

Stop-Transcript

# Ejecutar post-install si existe
$post = Join-Path $SourceDir "post-install.ps1"
if (Test-Path $post) {
    & $post
}