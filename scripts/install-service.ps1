#Requires -RunAsAdministrator

param(
    [string]$ServiceName = "FichaCostoService",
    [string]$DisplayName = "FichaCosto Service MVP",
    [string]$Description = "Automatizacion fichas de costo PyMEs",
    [string]$InstallPath = "C:\Program Files\FichaCostoService"
)

Write-Host "=== Instalador FichaCosto Service ===" -ForegroundColor Cyan

# 1. Admin check
if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "Ejecutar como Administrador"
    exit 1
}

# 2. Copiar archivos
Write-Host "Copiando archivos..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path $InstallPath | Out-Null
Copy-Item -Path "..\publish\*" -Destination $InstallPath -Recurse -Force

# 3. Crear Logs y permisos
$LogsDir = Join-Path $InstallPath "Logs"
New-Item -ItemType Directory -Force -Path $LogsDir | Out-Null
try {
    $sid = New-Object System.Security.Principal.SecurityIdentifier("S-1-5-20")
    $account = $sid.Translate([System.Security.Principal.NTAccount])
    $acl = Get-Acl $LogsDir
    $rule = New-Object System.Security.AccessControl.FileSystemAccessRule($account.Value, "Modify,Write", "ContainerInherit,ObjectInherit", "None", "Allow")
    $acl.SetAccessRule($rule)
    Set-Acl $LogsDir $acl
    Write-Host "✅ Permisos configurados para $($account.Value)" -ForegroundColor Green
} catch {
    Write-Warning "No se pudieron configurar permisos específicos"
}

# 4. Eliminar servicio existente
if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
    Write-Host "Eliminando servicio anterior..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

# 5. Crear servicio con New-Service (MÉTODO CONFIABLE)
Write-Host "Creando servicio Windows..." -ForegroundColor Yellow
$exePath = Join-Path $InstallPath "FichaCosto.Service.exe"

try {
    New-Service `
        -Name $ServiceName `
        -BinaryPathName "`"$exePath`" --environment Production" `
        -DisplayName $DisplayName `
        -Description $Description `
        -StartupType Automatic `
        -ErrorAction Stop
    
    Write-Host "✅ Servicio creado" -ForegroundColor Green
} catch {
    Write-Error "❌ Fallo al crear servicio: $_"
    exit 1
}

# 6. Configurar recuperación
sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/10000// | Out-Null

# 7. Iniciar
Write-Host "Iniciando servicio..." -ForegroundColor Green
Start-Service -Name $ServiceName
Start-Sleep -Seconds 3

# 8. Verificar
$svc = Get-Service -Name $ServiceName
if ($svc.Status -eq "Running") {
    Write-Host "✅ Servicio ejecutándose" -ForegroundColor Green
    try {
        $health = Invoke-RestMethod -Uri "http://localhost:5000/api/health" -TimeoutSec 5
        Write-Host "✅ Health Check: $($health.status)" -ForegroundColor Green
    } catch {
        Write-Warning "Servicio iniciado pero endpoint no responde aún"
    }
} else {
    Write-Error "❌ Estado: $($svc.Status)"
}