#Requires -RunAsAdministrator

param(
    [string]$InstallPath = "C:\Program Files\FichaCostoService",
    [string]$DataPath = "C:\ProgramData\FichaCosto",
    [int]$ServicePort = 5000
)

$LogFile = "$env:Temp\FichaCosto-Install-Logs\post-install-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logEntry = "[$timestamp] [$Level] $Message"
    Add-Content -Path $LogFile -Value $logEntry
    Write-Host $logEntry
}

# Crear directorio de logs si no existe
New-Item -ItemType Directory -Force -Path (Split-Path $LogFile) | Out-Null
Write-Log "Iniciando post-instalación de FichaCosto Service v0.6.2"


#6.5.6 - Implementar regla de firewall en PowerShell
function Add-FichaCostoFirewallRule {
    param([int]$Port)
    
    try {
        $ruleName = "FichaCosto Service (TCP $Port)"
        $existingRule = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
        
        if ($existingRule) {
            Write-Log "Regla de firewall ya existe, eliminando para recrear..."
            Remove-NetFirewallRule -DisplayName $ruleName
        }
        
        New-NetFirewallRule -DisplayName $ruleName `
            -Direction Inbound `
            -LocalPort $Port `
            -Protocol TCP `
            -Action Allow `
            -Profile Any `
            -Description "Permite conexiones al servicio FichaCosto en puerto $Port"
        
        Write-Log "Regla de firewall creada exitosamente para puerto $Port" "SUCCESS"
        return $true
    }
    catch {
        Write-Log "Error creando regla de firewall: $($_.Exception.Message)" "ERROR"
        return $false
    }
}

# Ejecutar
$firewallOk = Add-FichaCostoFirewallRule -Port $ServicePort


#6.5.7 - Configurar permisos de carpetas

function Set-FichaCostoPermissions {
    param([string]$DataFolder, [string]$LogsFolder)
    
    try {
        # Obtener SID del usuario del servicio (SYSTEM)
        $systemSID = New-Object System.Security.Principal.SecurityIdentifier "S-1-5-18"
        $systemUser = $systemSID.Translate([System.Security.Principal.NTAccount])
        
        # Configurar permisos para carpeta Logs
        $logsAcl = Get-Acl $LogsFolder
        $logsRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
            $systemUser, "Modify,Write,ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow"
        )
        $logsAcl.SetAccessRule($logsRule)
        Set-Acl $LogsFolder $logsAcl
        Write-Log "Permisos configurados para carpeta Logs: $LogsFolder" "SUCCESS"
        
        # Configurar permisos para carpeta Data (SQLite)
        $dataAcl = Get-Acl $DataFolder
        $dataRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
            $systemUser, "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow"
        )
        $dataAcl.SetAccessRule($dataRule)
        Set-Acl $DataFolder $dataAcl
        Write-Log "Permisos configurados para carpeta Data: $DataFolder" "SUCCESS"
        
        return $true
    }
    catch {
        Write-Log "Error configurando permisos: $($_.Exception.Message)" "ERROR"
        return $false
    }
}

# Ejecutar
$permsOk = Set-FichaCostoPermissions -DataFolder "$DataPath\Data" -LogsFolder "$DataPath\Logs"

#6.5.8 - Verificar e inicializar Base de Datos (Seed)
function Initialize-FichaCostoDatabase {
    param([string]$DbFolder, [string]$InstallFolder)
    
    $dbPath = Join-Path $DbFolder "fichacosto.db"
    
    try {
        if (Test-Path $dbPath) {
            Write-Log "Base de datos ya existe en: $dbPath" "INFO"
            return $true
        }
        
        Write-Log "Base de datos no encontrada. Inicializando..." "INFO"
        
        # Buscar script SQL de seed en el instalador
        $seedScript = Join-Path $InstallFolder "Data\SeedData.sql"
        
        if (-not (Test-Path $seedScript)) {
            Write-Log "Script SeedData.sql no encontrado en $InstallFolder" "WARNING"
            Write-Log "La BD se inicializará automáticamente al primer inicio del servicio" "INFO"
            return $true
        }
        
        # Ejecutar seed usando sqlite3 o el propio EF Core (alternativa)
        # Opción A: Si tienes sqlite3.exe disponible
        # & "sqlite3.exe" $dbPath ".read $seedScript"
        
        # Opción B: Crear archivo de flag para que el servicio haga el seed
        $flagFile = Join-Path $DbFolder ".seed-required"
        New-Item -ItemType File -Path $flagFile -Force | Out-Null
        Write-Log "Flag de inicialización creado. El servicio ejecutará seed al arrancar." "SUCCESS"
        
        return $true
    }
    catch {
        Write-Log "Error inicializando BD: $($_.Exception.Message)" "ERROR"
        return $false
    }
}

# Ejecutar
$dbOk = Initialize-FichaCostoDatabase -DbFolder "$DataPath\Data" -InstallFolder $InstallPath

#6.5.9 - Test de conectividad y resumen final
function Test-FichaCostoService {
    param([int]$Port, [int]$MaxRetries = 10)
    
    Write-Log "Esperando inicio del servicio para test de conectividad..."
    Start-Sleep -Seconds 5
    
    for ($i = 1; $i -le $MaxRetries; $i++) {
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:$Port/swagger" -TimeoutSec 5 -ErrorAction Stop
            Write-Log "Servicio respondiendo correctamente en puerto $Port (Intento $i)" "SUCCESS"
            return $true
        }
        catch {
            Write-Log "Intento $i/$MaxRetries - Servicio no disponible aún..."
            Start-Sleep -Seconds 3
        }
    }
    
    Write-Log "No se pudo verificar conectividad después de $MaxRetries intentos" "WARNING"
    return $false
}

# Resumen final
Write-Log "=== RESUMEN POST-INSTALACIÓN ===" "INFO"
Write-Log "Firewall: $(if($firewallOk){'OK'}else{'FALLÓ'})" $(if($firewallOk){"SUCCESS"}else{"WARNING"})
Write-Log "Permisos: $(if($permsOk){'OK'}else{'FALLÓ'})" $(if($permsOk){"SUCCESS"}else{"WARNING"})
Write-Log "Base de Datos: $(if($dbOk){'OK'}else{'FALLÓ'})" $(if($dbOk){"SUCCESS"}else{"WARNING"})

# Iniciar servicio si no está corriendo
$service = Get-Service -Name "FichaCostoService" -ErrorAction SilentlyContinue
if ($service -and $service.Status -ne 'Running') {
    Write-Log "Iniciando servicio..."
    Start-Service -Name "FichaCostoService"
    Start-Sleep -Seconds 2
}

# Test de conectividad (opcional, puede comentarse si no quieres esperar)
 Test-FichaCostoService -Port $ServicePort

Write-Log "Post-instalación completada. Log guardado en: $LogFile" "SUCCESS"