#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Reparación de Base de Datos FichaCosto via API REST
.DESCRIPTION
    Usa exclusivamente la API para verificar y poblar datos.
    No verifica archivos físicos - solo confía en la API.
#>
param(
    [string]$ServiceName = "FichaCostoService",
    [string]$ServiceUrl = "http://localhost:5000",
    [switch]$ForceReset,
    [int]$MaxRetries = 30,
    [int]$RetryDelaySeconds = 2
)

$ErrorActionPreference = "Stop"
$LogFile = "$env:Temp\FichaCosto-Install-Logs\seed-repair-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"

function Write-Log {
    param([string]$Message, [ValidateSet("INFO","SUCCESS","WARNING","ERROR")][string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logEntry = "[$timestamp] [$Level] $Message"
    $logDir = Split-Path $LogFile
    if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }
    Add-Content -Path $LogFile -Value $logEntry
    switch ($Level) {
        "SUCCESS" { Write-Host $logEntry -ForegroundColor Green }
        "WARNING" { Write-Host $logEntry -ForegroundColor Yellow }
        "ERROR"   { Write-Host $logEntry -ForegroundColor Red }
        default   { Write-Host $logEntry }
    }
}

Write-Log "=== Reparación BD via API ==="
Write-Log "Service URL: $ServiceUrl"
Write-Log "ForceReset: $ForceReset"

# 1. Verificar servicio
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if (-not $service) {
    Write-Log "ERROR: Servicio '$ServiceName' no encontrado" "ERROR"
    exit 1
}

# 2. Asegurar que el servicio esté corriendo
if ($service.Status -ne 'Running') {
    Write-Log "Iniciando servicio..."
    Start-Service -Name $ServiceName
    Start-Sleep -Seconds 3
}

# 3. ESPERAR SERVICIO DISPONIBLE
Write-Log "Esperando servicio disponible..."
$serviceReady = $false

for ($i = 1; $i -le $MaxRetries; $i++) {
    try {
        $response = Invoke-WebRequest -Uri "$ServiceUrl/api/health" -TimeoutSec 5 -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            $serviceReady = $true
            Write-Log "Servicio respondiendo (intento $i)" "SUCCESS"
            break
        }
    }
    catch {
        Write-Log "Intento $i/$MaxRetries - esperando..."
        Start-Sleep -Seconds $RetryDelaySeconds
    }
}

if (-not $serviceReady) {
    Write-Log "ERROR: Servicio no disponible" "ERROR"
    exit 1
}

# 4. VERIFICAR ESTADO DE LA BD via API (única fuente de verdad)
Write-Log "Verificando estado de base de datos via API..."
try {
    $checkResponse = Invoke-WebRequest -Uri "$ServiceUrl/api/admin/check-data" -TimeoutSec 10
    $checkData = $checkResponse.Content | ConvertFrom-Json
    
    Write-Log "API responde: tables=$($checkData.tables), clientes=$($checkData.clientes), hasData=$($checkData.hasData)" "INFO"
    
    # Si no hay tablas, el schema no existe - reiniciar para crearlo
    if ($checkData.tables -eq 0) {
        Write-Log "Schema no existe (0 tablas). Reiniciando servicio para crearlo..." "WARNING"
        Restart-Service -Name $ServiceName -Force
        Start-Sleep -Seconds 5
        
        # Re-verificar con reintentos
        for ($retry = 1; $retry -le 5; $retry++) {
            try {
                $checkResponse2 = Invoke-WebRequest -Uri "$ServiceUrl/api/admin/check-data" -TimeoutSec 10
                $checkData2 = $checkResponse2.Content | ConvertFrom-Json
                Write-Log "Re-verificación: tables=$($checkData2.tables)" "INFO"
                
                if ($checkData2.tables -gt 0) { break }
                Start-Sleep -Seconds 2
            }
            catch {
                Write-Log "Reintento $retry fallido..."
            }
        }
        
        if ($checkData2.tables -eq 0) {
            Write-Log "ERROR: Schema no se creó después de reiniciar. Verifique logs del servicio en C:\ProgramData\FichaCosto\Logs\" "ERROR"
            exit 1
        }
        
        $checkData = $checkData2
    }
    
    # Si tiene datos y no es ForceReset, terminar
    if ($checkData.hasData -and -not $ForceReset) {
        Write-Log "Base de datos ya contiene $($checkData.clientes) clientes. Omitiendo seed." "SUCCESS"
        exit 0
    }
}
catch {
    Write-Log "ERROR verificando BD: $($_.Exception.Message)" "ERROR"
    exit 1
}

# 5. EJECUTAR SEED VIA API
Write-Log "Ejecutando seed de datos via API..."

$seedBody = @{
    Clientes = @(
        @{
            NombreEmpresa = "Mercado Demo 'El Portal' S.R.L."
            CUIT = "30123456789"
            Direccion = "Av. Corrientes 1234, CABA"
            ContactoNombre = "Carlos Rodríguez"
            ContactoEmail = "admin@elportal.com.ar"
            ContactoTelefono = "011-4567-8900"
        },
        @{
            NombreEmpresa = "Almacén y Café 'La Esquina' S.A."
            CUIT = "30876543210"
            Direccion = "Av. Santa Fe 567, Palermo"
            ContactoNombre = "María González"
            ContactoEmail = "compras@laesquina.com"
            ContactoTelefono = "011-5678-9012"
        }
    )
    Productos = @(
        @{
            ClienteId = 1
            Codigo = "CAF-001"
            Nombre = "Café Espresso Doble"
            Descripcion = "Café de especialidad, 60ml, doble extracción"
            UnidadMedida = 5
        },
        @{
            ClienteId = 1
            Codigo = "SAN-001"
            Nombre = "Sándwich de Pollo Completo"
            Descripcion = "Pechuga grillada, lechuga, tomate, queso cheddar"
            UnidadMedida = 5
        }
    )
} | ConvertTo-Json -Depth 3

try {
    $seedResponse = Invoke-WebRequest -Uri "$ServiceUrl/api/admin/seed" `
        -Method POST `
        -ContentType "application/json" `
        -Body $seedBody `
        -TimeoutSec 30
    
    $seedResult = $seedResponse.Content | ConvertFrom-Json
    Write-Log "Seed ejecutado: $($seedResult.message)" "SUCCESS"
    
    if ($seedResult.skipped) {
        Write-Log "Seed omitido: $($seedResult.clientes) clientes ya existían" "WARNING"
    } else {
        Write-Log "Insertados: $($seedResult.clientes) clientes, $($seedResult.productos) productos" "SUCCESS"
    }
}
catch {
    Write-Log "ERROR en seed: $($_.Exception.Message)" "ERROR"
    if ($_.Exception.Response) {
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $reader.BaseStream.Position = 0
            $reader.DiscardBufferedData()
            $errorBody = $reader.ReadToEnd()
            Write-Log "Response: $errorBody" "ERROR"
        }
        catch {
            Write-Log "No se pudo leer response body" "ERROR"
        }
    }
    exit 1
}

# 6. VERIFICACIÓN FINAL
Write-Log "Verificación final..."
try {
    $finalCheck = Invoke-WebRequest -Uri "$ServiceUrl/api/admin/check-data" -TimeoutSec 5
    $finalData = $finalCheck.Content | ConvertFrom-Json
    Write-Log "Verificación final: $($finalData.clientes) clientes, $($finalData.tables) tablas en BD" "SUCCESS"
}
catch {
    Write-Log "No se pudo verificar datos finales" "WARNING"
}

Write-Log "=== Reparación completada ===" "SUCCESS"
Write-Log "Log: $LogFile" "INFO"