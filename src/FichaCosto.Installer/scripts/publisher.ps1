#Requires -RunAsAdministrator
param(
    [string]$Configuration = "Release",
    [string]$LogDir = "$env:TEMP\FichaCosto-Install-Logs"
)

$ErrorActionPreference = "Stop"
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$logFile = "$LogDir\02-publisher-$timestamp.log"

Start-Transcript -Path $logFile -Force

Write-Host "=== 02. PUBLICACIÓN DEL SERVICIO (x64) ===" -ForegroundColor Cyan

$basePath = "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP"
$projectPath = "$basePath\src\FichaCosto.Service\FichaCosto.Service.csproj"
$publishPath = "$basePath\src\FichaCosto.Service\bin\$Configuration\net9.0\win-x64\publish"

Write-Host "Configuración: $Configuration"
Write-Host "Ruta publish: $publishPath`n"

# Limpiar publish anterior
if (Test-Path $publishPath) {
    Write-Host "[1/4] Limpiando publicación anterior..." -ForegroundColor Yellow
    Remove-Item $publishPath -Recurse -Force -ErrorAction SilentlyContinue
}

# Publicar
Write-Host "`n[2/4] Publicando aplicación (x64)..." -ForegroundColor Yellow
dotnet publish $projectPath `
    -c $Configuration `
    -r win-x64 `
    --self-contained false `
    -o $publishPath `
    -p:PublishSingleFile=false `
    -p:IncludeNativeLibrariesForSelfExtract=true

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Error en dotnet publish" -ForegroundColor Red
    Stop-Transcript
    exit 1
}


# Verificar archivos críticos (CORREGIDO para Microsoft.Data.Sqlite)
Write-Host "`n[3/4] Verificando archivos críticos..." -ForegroundColor Yellow
$criticalFiles = @(
    "FichaCosto.Service.exe",
    "FichaCosto.Service.dll",
    "Microsoft.Data.Sqlite.dll",  # <-- CAMBIO: Era System.Data.SQLite.dll
    "e_sqlite3.dll"               # <-- Native library de SQLitePCLRaw
)

$allFound = $true
foreach ($file in $criticalFiles) {
    $filePath = Join-Path $publishPath $file
    if (Test-Path $filePath) {
        $size = (Get-Item $filePath).Length / 1KB
        Write-Host "  ✅ $file ($([math]::Round($size,1)) KB)" -ForegroundColor Green
    } else {
        Write-Host "  ❌ FALTA: $file" -ForegroundColor Red
        $allFound = $false
    }
}

# Verificar específicamente la native library para win-x64
$nativePath = "$publishPath\e_sqlite3.dll"
if (Test-Path $nativePath) {
    Write-Host "  ✅ Native library SQLite cargada (e_sqlite3.dll)" -ForegroundColor Green
} else {
    Write-Host "  ⚠️  e_sqlite3.dll no encontrado - la BD podría no funcionar" -ForegroundColor Yellow
}

# Copiar archivos de datos SQL
Write-Host "`n[4/4] Copiando archivos de datos..." -ForegroundColor Yellow
$dataPath = "$publishPath\Data"
New-Item -ItemType Directory -Force -Path $dataPath | Out-Null

$sqlFiles = @("Schema.sql", "SeedData.sql")
foreach ($sql in $sqlFiles) {
    $source = "$basePath\src\FichaCosto.Service\Data\$sql"
    $dest = "$dataPath\$sql"
    if (Test-Path $source) {
        Copy-Item $source $dest -Force
        Write-Host "  ✅ $sql copiado" -ForegroundColor Green
    } else {
        Write-Host "  ❌ No encontrado: $source" -ForegroundColor Red
    }
}

# Listar contenido final
Write-Host "`n=== CONTENIDO DE PUBLICACIÓN ===" -ForegroundColor Cyan
Get-ChildItem $publishPath -File | Select-Object Name, @{N="SizeKB";E={[math]::Round($_.Length/1KB,1)}} | Format-Table -AutoSize

Write-Host "`n✅ PUBLICACIÓN COMPLETADA" -ForegroundColor Green
Stop-Transcript