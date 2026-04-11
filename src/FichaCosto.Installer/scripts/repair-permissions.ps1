#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Repara permisos ACL de FichaCosto Service
.DESCRIPTION
    Restaura permisos correctos para Logs y Data cuando fallan
#>

param(
    [string]$InstallPath = "C:\Program Files\FichaCostoService"
)

Write-Host "=== REPARACION DE PERMISOS ===" -ForegroundColor Cyan

if (-not (Test-Path $InstallPath)) {
    throw "No se encuentra la instalación en $InstallPath"
}

# Función para reparar carpeta
function Repair-FolderPermissions {
    param([string]$FolderPath, [string]$Description)
    
    Write-Host "`nReparando: $Description ($FolderPath)" -ForegroundColor Yellow
    
    if (-not (Test-Path $FolderPath)) {
        New-Item -ItemType Directory -Path $FolderPath -Force | Out-Null
        Write-Host "  Creada carpeta" -ForegroundColor Green
    }
    
    # Método 1: icacls (más confiable)
    $result = icacls $FolderPath /grant "*S-1-5-18:(OI)(CI)F" 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✓ Permisos SYSTEM (SID) aplicados" -ForegroundColor Green
    } else {
        Write-Warning "icacls con SID fallo, intentando con nombre..."
        icacls $FolderPath /grant "NT AUTHORITY\SYSTEM:(OI)(CI)F" 2>&1 | Out-Null
    }
    
    # Administradores (ambos idiomas)
    icacls $FolderPath /grant "Administradores:(OI)(CI)F" 2>&1 | Out-Null
    icacls $FolderPath /grant "BUILTIN\Administrators:(OI)(CI)F" 2>&1 | Out-Null
    Write-Host "  ✓ Permisos Administradores aplicados" -ForegroundColor Green
    
    # Resetear herencia
    icacls $FolderPath /inheritance:e 2>&1 | Out-Null
    
    # Probar escritura
    $testFile = Join-Path $FolderPath "permission-test-$(Get-Random).tmp"
    try {
        "test" | Out-File $testFile -Force
        Remove-Item $testFile -Force
        Write-Host "  ✓ Test de escritura exitoso" -ForegroundColor Green
        return $true
    } catch {
        Write-Error "  ✗ No se puede escribir en la carpeta"
        return $false
    }
}

# Reparar Logs
$logsOk = Repair-FolderPermissions (Join-Path $InstallPath "Logs") "Carpeta de Logs"

# Reparar Data
$dataOk = Repair-FolderPermissions (Join-Path $InstallPath "Data") "Carpeta de Data"

# Reparar archivo de BD específico si existe
$dbPath = Join-Path $InstallPath "Data\fichacosto.db"
if (Test-Path $dbPath) {
    Write-Host "`nReparando archivo de base de datos..." -ForegroundColor Yellow
    icacls $dbPath /grant "*S-1-5-18:F" 2>&1 | Out-Null
    Write-Host "  ✓ Permisos aplicados a fichacosto.db" -ForegroundColor Green
}

# Resumen
Write-Host "`n=== RESULTADO ===" -ForegroundColor Cyan
if ($logsOk -and $dataOk) {
    Write-Host "✓ Permisos reparados correctamente" -ForegroundColor Green
    Write-Host "Reinicie el servicio si estaba detenido: Restart-Service FichaCostoService" -ForegroundColor Yellow
} else {
    Write-Host "✗ Algunos permisos no pudieron repararse" -ForegroundColor Red
}