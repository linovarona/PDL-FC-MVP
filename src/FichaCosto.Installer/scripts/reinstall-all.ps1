#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Reinstalación completa de FichaCosto Service
#>
param(
    [string]$ProjectRoot = "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP",
    [string]$InstallDir = "C:\Program Files\FichaCostoService",
    [string]$DataDir = "C:\ProgramData\FichaCosto"
)

$ErrorActionPreference = "Stop"

Write-Host "=== REINSTALACION FICHA COSTO SERVICE ===" -ForegroundColor Cyan

# 1. DETENER Y ELIMINAR SERVICIO
Write-Host "[1/8] Deteniendo servicio..." -ForegroundColor Yellow
Stop-Service FichaCostoService -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

$service = Get-Service FichaCostoService -ErrorAction SilentlyContinue
if ($service) {
    sc.exe delete FichaCostoService | Out-Null
    Write-Host "  Servicio eliminado" -ForegroundColor Green
}

# 2. CERRAR PROCESOS QUE BLOQUEAN ARCHIVOS
Write-Host "[2/8] Liberando archivos bloqueados..." -ForegroundColor Yellow
Get-Process -Name "FichaCosto.Service" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

# 3. LIMPIAR DIRECTORIOS
Write-Host "[3/8] Limpiando directorios..." -ForegroundColor Yellow
if (Test-Path $InstallDir) {
    Remove-Item -Path $InstallDir -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  Eliminado: $InstallDir" -ForegroundColor Green
}

# 4. CREAR ESTRUCTURA DE CARPETAS
Write-Host "[4/8] Creando estructura..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path "$InstallDir\Data" -Force | Out-Null
New-Item -ItemType Directory -Path "$DataDir\Data" -Force | Out-Null
New-Item -ItemType Directory -Path "$DataDir\Logs" -Force | Out-Null
Write-Host "  Carpetas creadas" -ForegroundColor Green

# 5. PUBLICAR (SIN DEPENDER DE BIN\RELEASE)
Write-Host "[5/8] Publicando aplicacion..." -ForegroundColor Yellow
$publishPath = "$env:TEMP\FichaCostoPublish_$(Get-Random)"

dotnet publish "$ProjectRoot\src\FichaCosto.Service\FichaCosto.Service.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -o $publishPath `
    --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Error "Fallo la publicacion"
    exit 1
}

Write-Host "  Publicado en: $publishPath" -ForegroundColor Green

# 6. COPIAR ARCHIVOS A INSTALACION
Write-Host "[6/8] Copiando a directorio de instalacion..." -ForegroundColor Yellow
Copy-Item -Path "$publishPath\*" -Destination $InstallDir -Recurse -Force

# Copiar Schema.sql si no está incluido
$schemaSource = "$ProjectRoot\src\FichaCosto.Service\Data\Schema.sql"
if (Test-Path $schemaSource) {
    Copy-Item $schemaSource "$InstallDir\Data\" -Force
    Write-Host "  Schema.sql copiado" -ForegroundColor Green
}

# 7. CONFIGURAR PERMISOS
Write-Host "[7/8] Configurando permisos..." -ForegroundColor Yellow
$system = New-Object System.Security.Principal.SecurityIdentifier "S-1-5-18"

foreach ($folder in @("$DataDir\Data", "$DataDir\Logs")) {
    $acl = Get-Acl $folder
    $acl.SetAccessRuleProtection($true, $false)
    $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
        $system, "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
    $acl.AddAccessRule($rule)
    Set-Acl $folder $acl
}
Write-Host "  Permisos configurados" -ForegroundColor Green

# 8. INSTALAR Y INICIAR SERVICIO
Write-Host "[8/8] Instalando servicio..." -ForegroundColor Yellow
New-Service -Name "FichaCostoService" `
    -BinaryPathName "`"$InstallDir\FichaCosto.Service.exe`"" `
    -DisplayName "FichaCosto Service" `
    -StartupType Automatic `
    -Description "Servicio de calculo de fichas de costo para PyMEs"

Start-Service FichaCostoService
Start-Sleep -Seconds 5

# Verificar
$svc = Get-Service FichaCostoService
if ($svc.Status -eq 'Running') {
    Write-Host "  Servicio ejecutandose" -ForegroundColor Green
} else {
    Write-Error "El servicio no inicio correctamente"
}

# Test de conectividad
try {
    $response = Invoke-WebRequest "http://localhost:5000/api/health" -TimeoutSec 5
    Write-Host "  Health check: OK" -ForegroundColor Green
} catch {
    Write-Warning "Health check fallo: $($_.Exception.Message)"
}

# Limpieza
Remove-Item -Path $publishPath -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "`n=== REINSTALACION COMPLETADA ===" -ForegroundColor Green
Write-Host "Instalacion: $InstallDir" -ForegroundColor Gray
Write-Host "Datos: $DataDir" -ForegroundColor Gray
Write-Host "API: http://localhost:5000/swagger" -ForegroundColor Gray