param(
    [Parameter(Mandatory=$true)]
    [string]$InstallDir
)

$ErrorActionPreference = "Stop"
$logFile = "$env:TEMP\FichaCosto-Seed-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"

function Write-Log {
    param([string]$Message)
    "$(Get-Date -Format 'HH:mm:ss') - $Message" | Tee-Object -FilePath $logFile -Append | Write-Host
}

try {
    Write-Log "=== SEED DATABASE v0.6.2 ==="
    
    $dataDir = Join-Path $InstallDir "Data"
    $dbPath = Join-Path $dataDir "fichacosto.db"
    $schemaFile = Join-Path $dataDir "Schema.sql"
    $seedFile = Join-Path $dataDir "SeedData.sql"
    
    Write-Log "DB Path: $dbPath"
    
    # Verificar archivos SQL existen
    if (-not (Test-Path $schemaFile)) { throw "Schema.sql no encontrado en $dataDir" }
    if (-not (Test-Path $seedFile)) { throw "SeedData.sql no encontrado en $dataDir" }
    
    # Buscar System.Data.SQLite.dll (debe estar en INSTALLFOLDER)
    $sqliteDll = Join-Path $InstallDir "System.Data.SQLite.dll"
    if (-not (Test-Path $sqliteDll)) {
        throw "System.Data.SQLite.dll no encontrado en $InstallDir"
    }
    
    Add-Type -Path $sqliteDll
    
    # Crear conexión (crea el archivo si no existe)
    $connStr = "Data Source=$dbPath;Version=3;New=True;"
    $conn = New-Object System.Data.SQLite.SQLiteConnection($connStr)
    $conn.Open()
    Write-Log "Conexión abierta"
    
    # Leer y ejecutar Schema
    $schema = Get-Content $schemaFile -Raw -Encoding UTF8
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $schema
    $cmd.ExecuteNonQuery() | Out-Null
    Write-Log "Schema ejecutado"
    
    # Leer y ejecutar SeedData
    $seed = Get-Content $seedFile -Raw -Encoding UTF8
    $cmd.CommandText = $seed
    $cmd.ExecuteNonQuery() | Out-Null
    Write-Log "SeedData ejecutado"
    
    # Verificación
    $cmd.CommandText = "SELECT COUNT(*) FROM Clientes"
    $count = $cmd.ExecuteScalar()
    Write-Log "VERIFICACIÓN: $count clientes en BD"
    
    $conn.Close()
    Write-Log "=== SEED COMPLETADO ==="
    
    exit 0
}
catch {
    Write-Log "ERROR: $_"
    Write-Log $_.ScriptStackTrace
    exit 1
}