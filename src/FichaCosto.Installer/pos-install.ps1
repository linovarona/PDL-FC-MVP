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

# 1. CONFIGURAR PERMISOS DE LOGS
Write-Host "`n[1/6] Configurando permisos de Logs..." -ForegroundColor Yellow
$logsPath = Join-Path $InstallPath "Logs"

try {
    # Crear directorio si no existe (por si acaso)
    if (-not (Test-Path $logsPath)) {
        New-Item -ItemType Directory -Path $logsPath -Force | Out-Null
        Write-Host "  ✓ Directorio Logs creado" -ForegroundColor Green
    }

    # Configurar permisos para LocalSystem (NT AUTHORITY\SYSTEM)
    $acl = Get-Acl $logsPath
    
    # Remover permisos heredados problemáticos
    $acl.SetAccessRuleProtection($true, $false)
    
    # Agregar permiso para SYSTEM (FullControl)
    $systemRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
        "NT AUTHORITY\SYSTEM",
        "FullControl",
        "ContainerInherit,ObjectInherit",
        "None",
        "Allow"
    )
    $acl.AddAccessRule($systemRule)
    
    # Agregar permiso para Administradores
    $adminRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
        "BUILTIN\Administrators",
        "FullControl",
        "ContainerInherit,ObjectInherit",
        "None",
        "Allow"
    )
    $acl.AddAccessRule($adminRule)
    
    Set-Acl $logsPath $acl
    
    # Crear archivo de prueba para verificar escritura
    $testFile = Join-Path $logsPath "install-test.log"
    "Log de instalacion - $(Get-Date)" | Out-File -FilePath $testFile -Force
    
    Write-Host "  ✓ Permisos configurados correctamente" -ForegroundColor Green
    Write-Host "    - SYSTEM: FullControl" -ForegroundColor Gray
    Write-Host "    - Administrators: FullControl" -ForegroundColor Gray
} catch {
    Write-Warning "Error configurando permisos de Logs: $_"
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

    # Configurar permisos para Data (similar a Logs)
    $acl = Get-Acl $dataPath
    $acl.SetAccessRuleProtection($true, $false)
    
    $systemRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
        "NT AUTHORITY\SYSTEM", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow"
    )
    $adminRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
        "BUILTIN\Administrators", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow"
    )
    $acl.AddAccessRule($systemRule)
    $acl.AddAccessRule($adminRule)
    Set-Acl $dataPath $acl

    # Crear base de datos vacía si no existe Schema.sql o si queremos crearla
    if (Test-Path $schemaPath) {
        Write-Host "  ℹ Schema.sql encontrado" -ForegroundColor Gray
        
        # Verificar si sqlite3 está disponible
        $sqlite = Get-Command sqlite3 -ErrorAction SilentlyContinue
        
        if ($sqlite -and -not (Test-Path $dbPath)) {
            # Crear BD desde schema
            & sqlite3 $dbPath ".read $schemaPath"
            Write-Host "  ✓ Base de datos creada desde Schema.sql" -ForegroundColor Green
        } elseif (-not (Test-Path $dbPath)) {
            # Crear archivo vacío para que la app lo inicialice
            New-Item -ItemType File -Path $dbPath -Force | Out-Null
            "SQLite format 3" | Out-File -FilePath $dbPath -Encoding utf8
            Write-Host "  ✓ Archivo de base de datos creado (para inicializacion por aplicacion)" -ForegroundColor Green
        } else {
            Write-Host "  ℹ Base de datos ya existe" -ForegroundColor Gray
        }
    } else {
        # Crear BD vacía si no hay schema
        if (-not (Test-Path $dbPath)) {
            New-Item -ItemType File -Path $dbPath -Force | Out-Null
            Write-Host "  ✓ Archivo de base de datos creado" -ForegroundColor Green
        }
    }
    
    # Configurar permisos específicos para el archivo de BD
    if (Test-Path $dbPath) {
        $acl = Get-Acl $dbPath
        $systemRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
            "NT AUTHORITY\SYSTEM", "FullControl", "None", "None", "Allow"
        )
        $acl.AddAccessRule($systemRule)
        Set-Acl $dbPath $acl
    }
    
} catch {
    Write-Warning "Error inicializando base de datos: $_"
}

# 3. CONFIGURAR FIREWALL (opcional, para acceso remoto)
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
        Write-Host "  ✓ Regla de firewall creada para puerto $ServicePort" -ForegroundColor Green
    } else {
        Write-Host "  ℹ Regla de firewall ya existe" -ForegroundColor Gray
    }
} catch {
    Write-Warning "No se pudo configurar firewall: $_"
}

# 4. VERIFICAR SERVICIO
Write-Host "`n[4/6] Verificando estado del servicio..." -ForegroundColor Yellow
$service = Get-Service -Name "FichaCostoService" -ErrorAction SilentlyContinue

if ($service) {
    Write-Host "  Estado del servicio: $($service.Status)" -ForegroundColor $(if($service.Status -eq 'Running'){'Green'}else{'Yellow'})
    
    if ($service.Status -ne 'Running') {
        Write-Host "  Intentando iniciar servicio..." -ForegroundColor Yellow
        Start-Service -Name "FichaCostoService" -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 3
        $service.Refresh()
        
        if ($service.Status -eq 'Running') {
            Write-Host "  ✓ Servicio iniciado correctamente" -ForegroundColor Green
        } else {
            Write-Warning "  ⚠ No se pudo iniciar el servicio. Verificar Event Viewer."
        }
    }
} else {
    Write-Error "  ✗ Servicio no encontrado"
}

# 5. VERIFICAR ENDPOINT HTTP
Write-Host "`n[5/6] Verificando endpoint HTTP..." -ForegroundColor Yellow
$maxRetries = 15
$retry = 0
$success = $false

while ($retry -lt $maxRetries -and -not $success) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:$ServicePort/swagger" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            $success = $true
            Write-Host "  ✓ Swagger UI accesible en http://localhost:$ServicePort/swagger" -ForegroundColor Green
        }
    } catch {
        $retry++
        if ($retry -lt $maxRetries) {
            Write-Host "  Esperando respuesta del servicio... ($retry/$maxRetries)" -ForegroundColor Gray
            Start-Sleep -Seconds 2
        }
    }
}

if (-not $success) {
    Write-Warning "  ⚠ No se pudo verificar endpoint HTTP. El servicio podria estar iniciando."
}

# 6. RESUMEN FINAL Y DIAGNOSTICO
Write-Host "`n[6/6] Generando resumen de instalacion..." -ForegroundColor Yellow

$summary = @"
=== RESUMEN DE INSTALACION ===
Fecha: $(Get-Date)
Ruta de instalacion: $InstallPath

ESTRUCTURA DE DIRECTORIOS:
$(Get-ChildItem $InstallPath -Recurse | Select-Object FullName, Length | Format-Table -AutoSize | Out-String)

ESTADO DEL SERVICIO:
$(Get-Service FichaCostoService | Select-Object Name, Status, StartType | Format-Table | Out-String)

PERMISOS DE CARPETAS CRITICAS:
Logs: $((Get-Acl (Join-Path $InstallPath "Logs")).Access | Where-Object {$_.IdentityReference -like "*SYSTEM*"} | Select-Object -First 1 | ForEach-Object {"OK"} else {"REVISION REQUERIDA"})
Data: $((Get-Acl (Join-Path $InstallPath "Data")).Access | Where-Object {$_.IdentityReference -like "*SYSTEM*"} | Select-Object -First 1 | ForEach-Object {"OK"} else {"REVISION REQUERIDA"})

URL DE ACCESO:
http://localhost:$ServicePort/swagger

LOGS DEL SISTEMA:
$(Get-WinEvent -FilterHashtable @{LogName='Application'; ProviderName='FichaCostoService'} -MaxEvents 5 -ErrorAction SilentlyContinue | Select-Object TimeCreated, LevelDisplayName, Message | Format-Table -Wrap | Out-String)

DIAGNOSTICO:
- Servicio: $(if($service.Status -eq 'Running'){'✓ Operativo'}else{'✗ No ejecutandose'})
- Logs: $(if(Test-Path (Join-Path $InstallPath "Logs\install-test.log")){'✓ Escritura verificada'}else{'✗ Sin permisos de escritura'})
- BD: $(if(Test-Path (Join-Path $InstallPath "Data\fichacosto.db")){'✓ Base de datos creada'}else{'✗ No inicializada'})
- HTTP: $(if($success){'✓ Endpoint responde'}else{'? Verificacion pendiente'})
"@

$summary | Out-File -FilePath "$LogDir\resumen-instalacion.txt" -Force
Write-Host $summary -ForegroundColor Cyan

Write-Host "`n=== POST-INSTALACION COMPLETADA ===" -ForegroundColor Cyan
Write-Host "Para diagnosticos futuros, revisar:" -ForegroundColor Gray
Write-Host "  - Logs de instalacion: $LogDir" -ForegroundColor Gray
Write-Host "  - Event Viewer: Aplicacion > FichaCostoService" -ForegroundColor Gray
Write-Host "  - Archivos de log: $(Join-Path $InstallPath 'Logs')" -ForegroundColor Gray

Stop-Transcript

# Retornar estado para automatizacion
return @{
    ServiceRunning = ($service.Status -eq 'Running')
    HttpAccessible = $success
    LogsWritable = Test-Path (Join-Path $InstallPath "Logs\install-test.log")
    DatabaseExists = Test-Path (Join-Path $InstallPath "Data\fichacosto.db")
}