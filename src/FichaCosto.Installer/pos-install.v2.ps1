#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Post-instalación: Configuración adicional y verificación
.DESCRIPTION
    Configura permisos, inicializa base de datos y verifica el servicio
#>

param(
    [string]$InstallPath = "C:\Program Files\FichaCostoService",
    [string]$LogDir = "$env:TEMP\FichaCosto-PostInstall",
    [int]$ServicePort = 5000
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Path $LogDir -Force | Out-Null
$LogFile = "$LogDir\post-install.log"
Start-Transcript -Path $LogFile -Force

Write-Host "=== FICHA COSTO SERVICE - POST-INSTALACION ===" -ForegroundColor Cyan

# 1. CONFIGURAR PERMISOS DE LOGS (USANDO ICACLS SÓLO - NO .NET ACL)
Write-Host "`n[1/6] Configurando permisos de Logs..." -ForegroundColor Yellow
$logsPath = Join-Path $InstallPath "Logs"

try {
    if (-not (Test-Path $logsPath)) {
        New-Item -ItemType Directory -Path $logsPath -Force | Out-Null
        Write-Host "  ✓ Directorio Logs creado" -ForegroundColor Green
    }

    # USAR ICACLS CON SID NUMÉRICO (funciona en cualquier idioma)
    # S-1-5-18 = SYSTEM
    # S-1-5-32-544 = Administrators
    
    $result = icacls "`"$logsPath`"" /grant "*S-1-5-18:(OI)(CI)F" /T 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "icacls SYSTEM fallo: $result"
    }
    Write-Host "  ✓ Permisos SYSTEM aplicados (SID)" -ForegroundColor Green
    
    # Administradores (intentar ambos nombres por si acaso)
    icacls "`"$logsPath`"" /grant "Administradores:(OI)(CI)F" /T 2>&1 | Out-Null
    icacls "`"$logsPath`"" /grant "*S-1-5-32-544:(OI)(CI)F" /T 2>&1 | Out-Null
    Write-Host "  ✓ Permisos Administradores aplicados" -ForegroundColor Green
    
    # Test de escritura
    $testFile = Join-Path $logsPath "post-install-test.log"
    "Test de permisos - $(Get-Date)" | Out-File -FilePath $testFile -Force -ErrorAction Stop
    Remove-Item $testFile -Force -ErrorAction SilentlyContinue
    Write-Host "  ✓ Test de escritura exitoso" -ForegroundColor Green
    
} catch {
    Write-Warning "  ⚠ Error en Logs: $_"
}

# 2. INICIALIZAR BASE DE DATOS SQLITE
Write-Host "`n[2/6] Inicializando base de datos SQLite..." -ForegroundColor Yellow
$dataPath = Join-Path $InstallPath "Data"
$dbPath = Join-Path $dataPath "fichacosto.db"
$schemaPath = Join-Path $dataPath "Schema.sql"

try {
    if (-not (Test-Path $dataPath)) {
        New-Item -ItemType Directory -Path $dataPath -Force | Out-Null
    }

    # Permisos con icacls (SIDs numéricos)
    icacls "`"$dataPath`"" /grant "*S-1-5-18:(OI)(CI)F" /T 2>&1 | Out-Null
    icacls "`"$dataPath`"" /grant "*S-1-5-32-544:(OI)(CI)F" /T 2>&1 | Out-Null
    Write-Host "  ✓ Permisos Data aplicados" -ForegroundColor Green

    if (-not (Test-Path $dbPath)) {
    Write-Host "  ℹ Base de datos no existe. Será creada por la aplicación al iniciar." -ForegroundColor Yellow
    # Solo asegurar que la carpeta tiene permisos para que la app cree el archivo
    
    } else {
        Write-Host "  ℹ Base de datos ya existe" -ForegroundColor Gray
    
    
    # Permisos específicos para el archivo DB
    icacls "`"$dbPath`"" /grant "*S-1-5-18:F" 2>&1 | Out-Null
    Write-Host "  ✓ Permisos aplicados a fichacosto.db" -ForegroundColor Green
    }
    
} catch {
    Write-Warning "  ⚠ Error en Data: $_"
}

# 3. CONFIGURAR FIREWALL
Write-Host "`n[3/6] Configurando reglas de firewall..." -ForegroundColor Yellow
try {
    $ruleName = "FichaCosto Service - Puerto $ServicePort"
    $existingRule = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
    
    if (-not $existingRule) {
        New-NetFirewallRule -DisplayName $ruleName `
            -Direction Inbound `
            -Protocol TCP `
            -LocalPort $ServicePort `
            -Action Allow `
            -Profile Any `
            -Description "Permitir acceso al FichaCosto Service" | Out-Null
        Write-Host "  ✓ Regla de firewall creada" -ForegroundColor Green
    } else {
        Write-Host "  ℹ Regla ya existe" -ForegroundColor Gray
    }
} catch {
    Write-Warning "  ⚠ Firewall: $_"
}

# 4. VERIFICAR SERVICIO
Write-Host "`n[4/6] Verificando servicio..." -ForegroundColor Yellow
$service = Get-Service -Name "FichaCostoService" -ErrorAction SilentlyContinue

if ($service) {
    Write-Host "  Estado: $($service.Status)" -ForegroundColor $(if($service.Status -eq 'Running'){'Green'}else{'Yellow'})
    
    if ($service.Status -ne 'Running') {
        Write-Host "  Iniciando..." -ForegroundColor Yellow
        Start-Service -Name "FichaCostoService" -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 3
        $service.Refresh()
        
        if ($service.Status -eq 'Running') {
            Write-Host "  ✓ Servicio iniciado" -ForegroundColor Green
        } else {
            Write-Warning "  ⚠ No inicio automaticamente"
        }
    }
} else {
    Write-Error "  ✗ Servicio no encontrado"
}

# 5. VERIFICAR HTTP
Write-Host "`n[5/6] Verificando endpoint HTTP..." -ForegroundColor Yellow
$maxRetries = 15
$retry = 0
$success = $false

while ($retry -lt $maxRetries -and -not $success) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:$ServicePort/swagger" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            $success = $true
            Write-Host "  ✓ Swagger UI accesible" -ForegroundColor Green
        }
    } catch {
        $retry++
        if ($retry -lt $maxRetries) {
            Write-Host "  Esperando... ($retry/$maxRetries)" -ForegroundColor Gray
            Start-Sleep -Seconds 2
        }
    }
}

if (-not $success) {
    Write-Warning "  ⚠ HTTP no responde aun"
}

# 6. RESUMEN FINAL (CORREGIDO - SIN FOR EACH OBJECT ELSE)
Write-Host "`n[6/6] Generando resumen..." -ForegroundColor Yellow

# Función helper para verificar
function Get-Status {
    param([string]$Path)
    if (-not (Test-Path $Path)) { return "NO EXISTE" }
    try {
        $acl = Get-Acl $Path -ErrorAction SilentlyContinue
        if (-not $acl) { return "SIN PERMISOS" }
        $hasSystem = $acl.Access | Where-Object { $_.IdentityReference -match "SYSTEM|S-1-5-18" } | Select-Object -First 1
        if ($hasSystem) { return "OK" } else { return "REVISION" }
    } catch { return "ERROR" }
}

$logsStatus = Get-Status (Join-Path $InstallPath "Logs")
$dataStatus = Get-Status (Join-Path $InstallPath "Data")

$summary = @"
=== RESUMEN ===
Fecha: $(Get-Date)
Ruta: $InstallPath

ESTRUCTURA:
Logs: $logsStatus
Data: $dataStatus
BD: $(if(Test-Path (Join-Path $InstallPath "Data\fichacosto.db")){"OK"}else{"NO EXISTE"})
Test Escritura: $(if(Test-Path (Join-Path $InstallPath "Logs\post-install-test.log")){"OK"}else{"FALLO"})

SERVICIO: $($service.Status)
HTTP: $(if($success){"OK"}else{"PENDIENTE"})
"@

Write-Host $summary -ForegroundColor Cyan
$summary | Out-File -FilePath "$LogDir\resumen.txt" -Force

Write-Host "`n✓ Post-instalacion completada" -ForegroundColor Green
Stop-Transcript