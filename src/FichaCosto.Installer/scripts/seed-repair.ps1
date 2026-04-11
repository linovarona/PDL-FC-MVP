#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Repara la base de datos ejecutando el SeedData.sql usando Microsoft.Data.Sqlite (EF Core)
#>
param(
    [string]$InstallDir = "C:\Program Files\FichaCostoService",
    [string]$LogDir = "$env:TEMP\FichaCosto-Install-Logs"
)

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$logFile = "$LogDir\08-seed-repair-$timestamp.log"
Start-Transcript -Path $logFile -Force

Write-Host "=== REPARACIÓN DE BASE DE DATOS (Microsoft.Data.Sqlite) ===" -ForegroundColor Cyan

$dataDir = "$InstallDir\Data"
$dbPath = "$dataDir\fichacosto.db"
$schemaFile = "$dataDir\Schema.sql"
$seedFile = "$dataDir\SeedData.sql"

# Verificar archivos
Write-Host "[1/5] Verificando archivos..." -ForegroundColor Yellow
if (-not (Test-Path $schemaFile)) { throw "No encontrado: Schema.sql" }
if (-not (Test-Path $seedFile)) { throw "No encontrado: SeedData.sql" }
Write-Host "  ✅ Archivos SQL encontrados" -ForegroundColor Green

# Detener servicio
Write-Host "`n[2/5] Deteniendo servicio..." -ForegroundColor Yellow
Stop-Service "FichaCostoService" -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
Write-Host "  ✅ Servicio detenido" -ForegroundColor Green

# Cargar Microsoft.Data.Sqlite
Write-Host "`n[3/5] Cargando Microsoft.Data.Sqlite..." -ForegroundColor Yellow
try {
    Add-Type -Path "$InstallDir\Microsoft.Data.Sqlite.dll" -ErrorAction Stop
    Write-Host "  ✅ Microsoft.Data.Sqlite cargado" -ForegroundColor Green
} catch {
    throw "No se pudo cargar Microsoft.Data.Sqlite.dll: $($_.Exception.Message)"
}

# Crear/recrear base de datos
Write-Host "`n[4/5] Creando base de datos..." -ForegroundColor Yellow

# Si existe, hacer backup y eliminar
if (Test-Path $dbPath) {
    $backupPath = "$dbPath.backup.$timestamp"
    Copy-Item $dbPath $backupPath -Force
    Write-Host "  📦 Backup creado: $backupPath" -ForegroundColor Gray
    Remove-Item $dbPath -Force
}

$connectionString = "Data Source=$dbPath"
$connection = New-Object Microsoft.Data.Sqlite.SqliteConnection($connectionString)
$connection.Open()

try {
    # Leer y ejecutar Schema
    $schemaSql = Get-Content $schemaFile -Raw -Encoding UTF8
    $command = $connection.CreateCommand()
    $command.CommandText = $schemaSql
    $command.ExecuteNonQuery() | Out-Null
    Write-Host "  ✅ Schema ejecutado" -ForegroundColor Green

    # Leer y ejecutar SeedData
    $seedSql = Get-Content $seedFile -Raw -Encoding UTF8
    $command.CommandText = $seedSql
    $command.ExecuteNonQuery() | Out-Null
    Write-Host "  ✅ SeedData ejecutado" -ForegroundColor Green

    # Verificación
    $command.CommandText = "SELECT COUNT(*) FROM Clientes"
    $count = $command.ExecuteScalar()
    Write-Host "`n📊 Verificación:" -ForegroundColor Cyan
    Write-Host "   - Clientes insertados: $count" -ForegroundColor White
    
    if ($count -eq 0) {
        throw "La base de datos quedó vacía después del seed"
    }

} finally {
    $connection.Close()
}

# Iniciar servicio
Write-Host "`n[5/5] Iniciando servicio..." -ForegroundColor Yellow
Start-Service "FichaCostoService"
Start-Sleep -Seconds 3

# Verificar HTTP
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5000/api/clientes" -TimeoutSec 10
    $data = $response.Content | ConvertFrom-Json
    Write-Host "  ✅ Servicio responde con $($data.Length) clientes" -ForegroundColor Green
} catch {
    Write-Host "  ⚠️  Servicio iniciado pero no responde HTTP todavía" -ForegroundColor Yellow
}

Write-Host "`n✅ REPARACIÓN COMPLETADA" -ForegroundColor Green
Stop-Transcript