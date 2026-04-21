#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Post-instalación FichaCosto MVP v0.6.2
.DESCRIPTION
    Configura firewall, permisos y ejecuta seed de datos vía API REST.
    No carga SQLite directamente - evita bloqueos de archivo.
#>
param(
    [string]$InstallPath = "C:\Program Files\FichaCostoService",
    [string]$DataPath = "C:\ProgramData\FichaCosto",
    [int]$ServicePort = 5000,
    [string]$ServiceName = "FichaCostoService",
    [int]$MaxRetries = 30,
    [int]$RetryDelaySeconds = 2
)

$ErrorActionPreference = "Stop"
$LogFile = "$env:Temp\FichaCosto-Install-Logs\post-install-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
$ServiceUrl = "http://localhost:$ServicePort"

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

Write-Log "=== Post-instalación FichaCosto MVP v0.6.2 ==="
Write-Log "Service URL: $ServiceUrl"
Write-Log "Data Path: $DataPath"

# 1. FIREWALL
Write-Log "Configurando regla de firewall para puerto $ServicePort..."
try {
    $ruleName = "FichaCosto Service (TCP $ServicePort)"
    Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue | Remove-NetFirewallRule
    New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -LocalPort $ServicePort `
        -Protocol TCP -Action Allow -Profile Any -Enabled True | Out-Null
    Write-Log "Firewall configurado" "SUCCESS"
} catch {
    Write-Log "ERROR Firewall: $($_.Exception.Message)" "ERROR"
}

# 2. PERMISOS DE CARPETAS
Write-Log "Configurando permisos de carpetas..."
try {
    $logsFolder = Join-Path $DataPath "Logs"
    $dbFolder = Join-Path $DataPath "Data"
    
    New-Item -ItemType Directory -Path $logsFolder -Force | Out-Null
    New-Item -ItemType Directory -Path $dbFolder -Force | Out-Null
    
    $system = New-Object System.Security.Principal.SecurityIdentifier "S-1-5-18"
    $admins = New-Object System.Security.Principal.SecurityIdentifier "S-1-5-32-544"
    
    foreach ($folder in @($logsFolder, $dbFolder)) {
        $acl = Get-Acl $folder
        $acl.SetAccessRuleProtection($true, $false)
        
        $ruleSystem = New-Object System.Security.AccessControl.FileSystemAccessRule(
            $system, "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
        $acl.AddAccessRule($ruleSystem)
        
        $ruleAdmin = New-Object System.Security.AccessControl.FileSystemAccessRule(
            $admins, "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
        $acl.AddAccessRule($ruleAdmin)
        
        Set-Acl $folder $acl
    }
    Write-Log "Permisos configurados" "SUCCESS"
} catch {
    Write-Log "ERROR Permisos: $($_.Exception.Message)" "ERROR"
}

# 3. INICIAR SERVICIO
Write-Log "Iniciando servicio '$ServiceName'..."
try {
    $service = Get-Service -Name $ServiceName -ErrorAction Stop
    if ($service.Status -ne 'Running') {
        Start-Service -Name $ServiceName
    }
    Write-Log "Servicio iniciado" "SUCCESS"
} catch {
    Write-Log "ERROR Servicio: $($_.Exception.Message)" "ERROR"
    exit 1
}

# 4. ESPERAR SERVICIO DISPONIBLE
Write-Log "Esperando servicio disponible (máx $MaxRetries intentos)..."
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
    Write-Log "ERROR: Servicio no disponible después de $MaxRetries intentos" "ERROR"
    exit 1
}

# 5. VERIFICAR SI YA TIENE DATOS
Write-Log "Verificando estado de base de datos..."
try {
    $checkResponse = Invoke-WebRequest -Uri "$ServiceUrl/api/admin/check-data" -TimeoutSec 10
    $checkData = $checkResponse.Content | ConvertFrom-Json
    
    if ($checkData.hasData) {
        Write-Log "Base de datos ya contiene datos ($($checkData.clientes) clientes). Omitiendo seed." "SUCCESS"
        exit 0
    }
    Write-Log "Base de datos vacía. Ejecutando seed..." "INFO"
}
catch {
    Write-Log "No se pudo verificar datos, intentando seed de todas formas..." "WARNING"
}

# 6. EJECUTAR SEED VIA API
Write-Log "Ejecutando seed de datos vía API..."
$seedBody = @{
    Clientes = @(
        @{
            NombreEmpresa = "Mercado Demo 'El Portal' S.R.L."
            CUIT = "30123456789"
            Direccion = "Av. Corrientes 1234, CABA"
            ContactoNombre = "Carlos Rodríguez"
            ContactoEmail = "admin@elportal.com.ar"
            ContactoTelefono = "011-4567-8900"
        }
    )
    Productos = @(
        @{
            ClienteId = 1
            Codigo = "CAF-001"
            Nombre = "Café Espresso Doble"
            Descripcion = "Café de especialidad, 60ml, doble extracción"
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
}
catch {
    Write-Log "ERROR en seed: $($_.Exception.Message)" "ERROR"
    Write-Log "El servicio funciona pero sin datos de ejemplo." "WARNING"
    Write-Log "Use Swagger UI ($ServiceUrl/swagger) para crear datos manualmente." "INFO"
}

# 7. VERIFICACIÓN FINAL
Write-Log "Verificación final..."
try {
    $finalCheck = Invoke-WebRequest -Uri "$ServiceUrl/api/admin/check-data" -TimeoutSec 5
    $finalData = $finalCheck.Content | ConvertFrom-Json
    Write-Log "Verificación: $($finalData.clientes) clientes en base de datos" "SUCCESS"
}
catch {
    Write-Log "No se pudo verificar datos finales" "WARNING"
}

# 8. ELIMINAR FLAG SI EXISTE
$flagFile = Join-Path $DataPath "Data\.seed-required"
if (Test-Path $flagFile) {
    Remove-Item $flagFile -Force
    Write-Log "Flag de seed eliminado" "SUCCESS"
}

Write-Log "=== Post-instalación completada ===" "SUCCESS"
Write-Log "Acceso: $ServiceUrl/swagger" "INFO"
Write-Log "Log: $LogFile" "INFO"