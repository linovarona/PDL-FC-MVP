#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Diagnóstico completo de FichaCosto Service
.DESCRIPTION
    Verifica estado del servicio, permisos, base de datos, puertos y configuración
#>

param(
    [string]$InstallPath = "C:\Program Files\FichaCostoService",
    [int]$ServicePort = 5000
)

Write-Host "=== DIAGNOSTICO FICHA COSTO SERVICE ===" -ForegroundColor Cyan
Write-Host "Fecha: $(Get-Date)`n" -ForegroundColor Gray

$diagnostics = @()
$allOk = $true

# 1. VERIFICAR SERVICIO
Write-Host "[1/8] Verificando servicio Windows..." -ForegroundColor Yellow
$service = Get-Service -Name "FichaCostoService" -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "  ✓ Servicio encontrado" -ForegroundColor Green
    Write-Host "    Estado: $($service.Status)" -ForegroundColor $(if($service.Status -eq 'Running'){'Green'}else{'Red'})
    Write-Host "    Inicio: $($service.StartType)" -ForegroundColor Gray
    
    $diagnostics += @{ Test = "Servicio Existe"; Result = $true; Detail = "Status: $($service.Status)" }
    
    if ($service.Status -eq 'Running') {
        # Verificar proceso
        $proc = Get-Process -Name "FichaCosto.Service" -ErrorAction SilentlyContinue
        if ($proc) {
            Write-Host "    PID: $($proc.Id), Memoria: $([math]::Round($proc.WorkingSet64/1MB,2)) MB" -ForegroundColor Gray
        }
    }
} else {
    Write-Host "  ✗ Servicio NO encontrado" -ForegroundColor Red
    $diagnostics += @{ Test = "Servicio Existe"; Result = $false; Detail = "No instalado" }
    $allOk = $false
}

# 2. VERIFICAR ESTRUCTURA DE CARPETAS
Write-Host "`n[2/8] Verificando estructura de archivos..." -ForegroundColor Yellow
$requiredPaths = @{
    "Instalación" = $InstallPath
    "Ejecutable" = Join-Path $InstallPath "FichaCosto.Service.exe"
    "Logs" = Join-Path $InstallPath "Logs"
    "Data" = Join-Path $InstallPath "Data"
    "Appsettings" = Join-Path $InstallPath "appsettings.json"
}

foreach ($item in $requiredPaths.GetEnumerator()) {
    $exists = Test-Path $item.Value
    $icon = if ($exists) { "✓" } else { "✗" }
    $color = if ($exists) { "Green" } else { "Red" }
    Write-Host "  $icon $($item.Key): $($item.Value)" -ForegroundColor $color
    
    if (-not $exists -and $item.Key -ne "Appsettings") { $allOk = $false }
    $diagnostics += @{ Test = $item.Key; Result = $exists; Detail = $item.Value }
}

# 3. VERIFICAR PERMISOS ACL
Write-Host "`n[3/8] Verificando permisos ACL..." -ForegroundColor Yellow
$folders = @("Logs", "Data")
foreach ($folder in $folders) {
    $fullPath = Join-Path $InstallPath $folder
    if (Test-Path $fullPath) {
        try {
            $acl = Get-Acl $fullPath
            $systemAccess = $acl.Access | Where-Object { 
                $_.IdentityReference -match "SYSTEM|S-1-5-18" -and $_.FileSystemRights -match "Modify|FullControl|Write" 
            }
            
            if ($systemAccess) {
                Write-Host "  ✓ $folder:SYSTEM tiene permisos de escritura" -ForegroundColor Green
                $diagnostics += @{ Test = "Permisos $folder"; Result = $true }
            } else {
                Write-Host "  ✗ $folder:SYSTEM sin permisos de escritura" -ForegroundColor Red
                $diagnostics += @{ Test = "Permisos $folder"; Result = $false }
                $allOk = $false
            }
        } catch {
            Write-Host "  ✗ $folder:Error leyendo ACL" -ForegroundColor Red
        }
    }
}

# 4. VERIFICAR BASE DE DATOS SQLITE
Write-Host "`n[4/8] Verificando base de datos..." -ForegroundColor Yellow
$dbPath = Join-Path $InstallPath "Data\fichacosto.db"
if (Test-Path $dbPath) {
    $size = (Get-Item $dbPath).Length
    Write-Host "  ✓ fichacosto.db existe ($size bytes)" -ForegroundColor Green
    
    # Verificar si es válida (header SQLite)
    $bytes = [System.IO.File]::ReadAllBytes($dbPath)[0..5]
    $header = [System.Text.Encoding]::ASCII.GetString($bytes)
    if ($header -eq "SQLite") {
        Write-Host "    Header SQLite válido" -ForegroundColor Green
        $diagnostics += @{ Test = "Base de Datos"; Result = $true; Detail = "Válida, $size bytes" }
    } else {
        Write-Host "    ⚠ Header no es SQLite (posiblemente vacía)" -ForegroundColor Yellow
        $diagnostics += @{ Test = "Base de Datos"; Result = $true; Detail = "Vacía o nueva" }
    }
} else {
    Write-Host "  ✗ fichacosto.db NO existe" -ForegroundColor Red
    $diagnostics += @{ Test = "Base de Datos"; Result = $false }
    $allOk = $false
}

# 5. VERIFICAR PUERTO
Write-Host "`n[5/8] Verificando puerto $ServicePort..." -ForegroundColor Yellow
$portInUse = Get-NetTCPConnection -LocalPort $ServicePort -ErrorAction SilentlyContinue
if ($portInUse) {
    $proc = Get-Process -Id $portInUse.OwningProcess -ErrorAction SilentlyContinue
    Write-Host "  ⚠ Puerto $ServicePort en uso por: $($proc.ProcessName) (PID: $($portInUse.OwningProcess))" -ForegroundColor Yellow
    
    if ($proc.ProcessName -eq "FichaCosto.Service") {
        Write-Host "    ✓ Es el servicio correcto" -ForegroundColor Green
        $diagnostics += @{ Test = "Puerto"; Result = $true; Detail = "En uso por servicio" }
    } else {
        Write-Host "    ✗ Conflicto: otro proceso usa el puerto" -ForegroundColor Red
        $diagnostics += @{ Test = "Puerto"; Result = $false; Detail = "Conflicto con $($proc.ProcessName)" }
        $allOk = $false
    }
} else {
    Write-Host "  ⚠ Puerto $ServicePort libre (servicio no escuchando)" -ForegroundColor Yellow
    $diagnostics += @{ Test = "Puerto"; Result = $false; Detail = "Libre" }
}

# 6. VERIFICAR FIREWALL
Write-Host "`n[6/8] Verificando reglas de firewall..." -ForegroundColor Yellow
$fwRule = Get-NetFirewallRule -DisplayName "*FichaCosto*" -ErrorAction SilentlyContinue
if ($fwRule) {
    Write-Host "  ✓ Regla de firewall encontrada: $($fwRule.DisplayName)" -ForegroundColor Green
    $diagnostics += @{ Test = "Firewall"; Result = $true }
} else {
    Write-Host "  ⚠ No hay regla de firewall específica" -ForegroundColor Yellow
    $diagnostics += @{ Test = "Firewall"; Result = $false }
}

# 7. TEST HTTP ENDPOINT
Write-Host "`n[7/8] Probando endpoint HTTP..." -ForegroundColor Yellow
if ($service -and $service.Status -eq 'Running') {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:$ServicePort/api/health" -UseBasicParsing -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            Write-Host "  ✓ Health Check responde OK" -ForegroundColor Green
            Write-Host "    Respuesta: $($response.Content)" -ForegroundColor Gray
            $diagnostics += @{ Test = "HTTP Endpoint"; Result = $true }
        }
    } catch {
        Write-Host "  ✗ Health Check no responde: $_" -ForegroundColor Red
        $diagnostics += @{ Test = "HTTP Endpoint"; Result = $false; Detail = $_.Exception.Message }
        $allOk = $false
    }
} else {
    Write-Host "  ⚠ Servicio no ejecutándose, no se puede probar HTTP" -ForegroundColor Yellow
}

# 8. VERIFICAR EVENT LOGS
Write-Host "`n[8/8] Verificando Event Logs..." -ForegroundColor Yellow
$events = Get-WinEvent -FilterHashtable @{LogName='Application'; ProviderName='FichaCostoService'} -MaxEvents 3 -ErrorAction SilentlyContinue
if ($events) {
    Write-Host "  ℹ Últimos eventos del servicio:" -ForegroundColor Cyan
    $events | ForEach-Object {
        $color = if ($_.LevelDisplayName -eq 'Error') { 'Red' } elseif ($_.LevelDisplayName -eq 'Warning') { 'Yellow' } else { 'Gray' }
        Write-Host "    [$($_.LevelDisplayName)] $($_.TimeCreated): $($_.Message.Substring(0,[Math]::Min(60,$_.Message.Length)))..." -ForegroundColor $color
    }
} else {
    Write-Host "  ℹ No hay eventos recientes" -ForegroundColor Gray
}

# RESUMEN
Write-Host "`n=== RESUMEN DE DIAGNOSTICO ===" -ForegroundColor Cyan
Write-Host "Total pruebas: $($diagnostics.Count)" -ForegroundColor Gray
Write-Host "Exitosas: $(($diagnostics | Where-Object Result).Count)" -ForegroundColor Green
Write-Host "Fallidas: $(($diagnostics | Where-Object { -not $_.Result }).Count)" -ForegroundColor $(if($allOk){'Gray'}else{'Red'})

if ($allOk) {
    Write-Host "`n✓ Sistema operativo correctamente" -ForegroundColor Green
} else {
    Write-Host "`n✗ Se detectaron problemas. Recomendaciones:" -ForegroundColor Red
    $failed = $diagnostics | Where-Object { -not $_.Result }
    foreach ($fail in $failed) {
        Write-Host "  - $($fail.Test): $($fail.Detail)" -ForegroundColor Yellow
    }
    
    Write-Host "`nAcciones sugeridas:" -ForegroundColor Cyan
    if (($failed | Where-Object { $_.Test -match "Servicio" }).Count -gt 0) {
        Write-Host "  1. Ejecutar: .\install.ps1" -ForegroundColor White
    }
    if (($failed | Where-Object { $_.Test -match "Permisos" }).Count -gt 0) {
        Write-Host "  2. Ejecutar: .\repair-permissions.ps1" -ForegroundColor White
    }
    if (($failed | Where-Object { $_.Test -match "Puerto" -and $_.Detail -match "Conflicto" }).Count -gt 0) {
        Write-Host "  3. Cambiar puerto en appsettings.json o detener proceso conflictivo" -ForegroundColor White
    }
}

# Exportar resultado
$reportPath = "$env:TEMP\FichaCosto-Diagnose-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
$diagnostics | ConvertTo-Json | Out-File $reportPath
Write-Host "`nReporte guardado en: $reportPath" -ForegroundColor Gray