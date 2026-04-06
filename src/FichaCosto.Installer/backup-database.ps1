#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Realiza backup de la base de datos SQLite
.DESCRIPTION
    Crea copia de seguridad de fichacosto.db con timestamp
#>

param(
    [string]$InstallPath = "C:\Program Files\FichaCostoService",
    [string]$BackupDir = "$env:USERPROFILE\Documents\FichaCosto-Backups",
    [switch]$IncludeLogs  # Incluir también logs de la aplicación
)

Write-Host "=== BACKUP FICHA COSTO SERVICE ===" -ForegroundColor Cyan

$dbPath = Join-Path $InstallPath "Data\fichacosto.db"

if (-not (Test-Path $dbPath)) {
    throw "Base de datos no encontrada en $dbPath"
}

# Crear directorio de backup
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$backupSubdir = Join-Path $BackupDir $timestamp
New-Item -ItemType Directory -Path $backupSubdir -Force | Out-Null

Write-Host "Origen: $dbPath" -ForegroundColor Gray
Write-Host "Destino: $backupSubdir" -ForegroundColor Gray

# Detener servicio temporalmente para backup limpio
$service = Get-Service FichaCostoService -ErrorAction SilentlyContinue
$wasRunning = $false

if ($service -and $service.Status -eq 'Running') {
    Write-Host "`nDeteniendo servicio temporalmente..." -ForegroundColor Yellow
    Stop-Service FichaCostoService
    $wasRunning = $true
    Start-Sleep -Seconds 2
}

try {
    # Backup de base de datos
    $backupName = "fichacosto-$timestamp.db"
    $destPath = Join-Path $backupSubdir $backupName
    
    Copy-Item -Path $dbPath -Destination $destPath -Force
    Write-Host "✓ Base de datos copiada: $backupName" -ForegroundColor Green
    
    # Verificar integridad si sqlite3 disponible
    $sqlite = Get-Command sqlite3 -ErrorAction SilentlyContinue
    if ($sqlite) {
        $result = & sqlite3 $destPath "PRAGMA integrity_check;" 2>&1
        if ($result -eq "ok") {
            Write-Host "✓ Integridad verificada" -ForegroundColor Green
        } else {
            Write-Warning "Problemas de integridad detectados: $result"
        }
    }
    
    # Backup de Schema.sql si existe
    $schemaPath = Join-Path $InstallPath "Data\Schema.sql"
    if (Test-Path $schemaPath) {
        Copy-Item $schemaPath $backupSubdir
        Write-Host "✓ Schema.sql copiado" -ForegroundColor Green
    }
    
    # Backup de configuración
    $configPath = Join-Path $InstallPath "appsettings.json"
    if (Test-Path $configPath) {
        Copy-Item $configPath $backupSubdir
        Write-Host "✓ appsettings.json copiado" -ForegroundColor Green
    }
    
    # Backup de logs (opcional)
    if ($IncludeLogs) {
        $logsPath = Join-Path $InstallPath "Logs"
        if (Test-Path $logsPath) {
            $logsBackup = Join-Path $backupSubdir "Logs"
            Copy-Item $logsPath $logsBackup -Recurse
            Write-Host "✓ Logs incluidos en el backup" -ForegroundColor Green
        }
    }
    
    # Crear info del backup
    $info = @{
        Fecha = Get-Date
        VersionServicio = (Get-Item (Join-Path $InstallPath "FichaCostoService.exe")).VersionInfo.FileVersion
        TamañoBD = (Get-Item $destPath).Length
        Origen = $InstallPath
    } | ConvertTo-Json
    
    $info | Out-File (Join-Path $backupSubdir "backup-info.json")
    
    Write-Host "`n=== BACKUP COMPLETADO ===" -ForegroundColor Cyan
    Write-Host "Ubicación: $backupSubdir" -ForegroundColor Green
    Write-Host "Para restaurar, copie $backupName a Data\fichacosto.db" -ForegroundColor Gray
    
} finally {
    # Restaurar servicio si estaba corriendo
    if ($wasRunning) {
        Write-Host "`nReiniciando servicio..." -ForegroundColor Yellow
        Start-Service FichaCostoService
        Write-Host "✓ Servicio reiniciado" -ForegroundColor Green
    }
}

# Listar backups existentes
Write-Host "`nBackups disponibles:" -ForegroundColor Cyan
Get-ChildItem $BackupDir -Directory | Sort-Object CreationTime -Descending | 
    Select-Object -First 5 | 
    ForEach-Object {
        $size = (Get-ChildItem $_.FullName -Recurse | Measure-Object -Property Length -Sum).Sum
        Write-Host "  - $($_.Name) ($([math]::Round($size/1KB,2)) KB)" -ForegroundColor Gray
    }