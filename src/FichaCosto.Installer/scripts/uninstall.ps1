#Requires -RunAsAdministrator
param(
    [string]$ServiceName = "FichaCostoService",
    [string]$InstallPath = "C:\Program Files\FichaCostoService",
    [int]$MaxRetries = 3
)

$ErrorActionPreference = "Stop"
$LogFile = "$env:Temp\FichaCosto-Uninstall-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logEntry = "[$timestamp] [$Level] $Message"
    Add-Content -Path $LogFile -Value $logEntry
    Write-Host $logEntry
}

Write-Log "=== DESINSTALACIÓN FICHA COSTO ==="

# 1. Detener servicio (con reintentos)
Write-Log "Deteniendo servicio $ServiceName..."
$retry = 0
$stopped = $false

do {
    try {
        $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
        if (-not $service) {
            Write-Log "Servicio no existe" "WARNING"
            $stopped = $true
            break
        }
        
        if ($service.Status -eq 'Stopped') {
            Write-Log "Servicio ya detenido" "SUCCESS"
            $stopped = $true
            break
        }
        
        Stop-Service -Name $ServiceName -Force -ErrorAction Stop
        Start-Sleep -Seconds 3
        
        # Verificar que realmente se detuvo
        $service.Refresh()
        if ($service.Status -eq 'Stopped') {
            $stopped = $true
            Write-Log "Servicio detenido" "SUCCESS"
        }
    }
    catch {
        $retry++
        Write-Log "Intento $retry/$MaxRetries fallido: $($_.Exception.Message)" "WARNING"
        Start-Sleep -Seconds 2
    }
} while (-not $stopped -and $retry -lt $MaxRetries)

if (-not $stopped) {
    Write-Log "ADVERTENCIA: No se pudo detener el servicio limpiamente" "WARNING"
    Write-Log "Intentando matar proceso..." "WARNING"
    
    # Último recurso: matar proceso
    Get-Process -Name "FichaCosto.Service" -ErrorAction SilentlyContinue | 
        Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 3
}

# 2. Desinstalar servicio
try {
    if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
        sc.exe delete $ServiceName | Out-Null
        Write-Log "Servicio eliminado del SCM" "SUCCESS"
    }
} catch {
    Write-Log "Error eliminando servicio: $($_.Exception.Message)" "ERROR"
}

# 3. Eliminar archivos (con manejo de DLLs bloqueadas)
Write-Log "Eliminando archivos de $InstallPath..."
$filesDeleted = 0
$filesFailed = @()

if (Test-Path $InstallPath) {
    # Intentar eliminar varias veces con esperas
    for ($i = 0; $i -lt 3; $i++) {
        try {
            # Forzar liberación de handles (si hay alguno abierto)
            [System.GC]::Collect()
            [System.GC]::WaitForPendingFinalizers()
            
            Remove-Item -Path $InstallPath -Recurse -Force -ErrorAction Stop
            Write-Log "Carpeta eliminada completamente" "SUCCESS"
            $filesDeleted = 1
            break
        }
        catch {
            Write-Log "Intento $($i+1): Algunos archivos en uso..." "WARNING"
            
            # Estrategia: eliminar lo que se pueda, ignorar lo bloqueado
            Get-ChildItem $InstallPath -Recurse -File | ForEach-Object {
                try {
                    Remove-Item $_.FullName -Force -ErrorAction Stop
                    $filesDeleted++
                }
                catch {
                    $filesFailed += $_.Name
                }
            }
            
            Start-Sleep -Seconds 5
        }
    }
}

if ($filesFailed.Count -gt 0) {
    Write-Log "Archivos no eliminados (en uso): $($filesFailed -join ', ')" "WARNING"
    Write-Log "Se eliminarán al reiniciar Windows (usando PendingFileRenameOperations)" "INFO"
    
    # Registrar para eliminación al reinicio (método alternativo)
    foreach ($file in $filesFailed) {
        $fullPath = Join-Path $InstallPath $file
        if (Test-Path $fullPath) {
            # Usar MoveFileEx para eliminar al reinicio
            $signature = @'
[DllImport("kernel32.dll", CharSet=CharSet.Unicode, SetLastError=true)]
public static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName, int dwFlags);
'@
            $mov = Add-Type -MemberDefinition $signature -Name "MovFileEx" -PassThru
            $MOVEFILE_DELAY_UNTIL_REBOOT = 0x4
            $mov::MoveFileEx($fullPath, $null, $MOVEFILE_DELAY_UNTIL_REBOOT) | Out-Null
        }
    }
}

# 4. Eliminar datos (opcional - preguntar)
Write-Log "¿Eliminar datos en C:\ProgramData\FichaCosto? (S/N)" "INFO"
# Aquí podrías agregar Read-Host si es interactivo, o un parámetro -PurgeData

Write-Log "=== DESINSTALACIÓN COMPLETADA ===" "SUCCESS"
Write-Log "Log: $LogFile" "INFO"

if ($filesFailed.Count -gt 0) {
    Write-Log "NOTA: Algunos archivos se eliminarán al reiniciar la computadora" "WARNING"
}