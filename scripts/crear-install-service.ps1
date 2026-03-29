@'
#Requires -RunAsAdministrator

param(
    [string]$ServiceName = "FichaCostoService",
    [string]$DisplayName = "FichaCosto Service MVP",
    [string]$Description = "Automatización de fichas de costo PyMEs (Res. 148/2023, 209/2024)",
    [string]$InstallPath = "C:\Program Files\FichaCostoService",
    [string]$Port = "5000"
)

Write-Host "=== Instalador FichaCosto Service v1.0.0-MVP (.NET 9.0) ===" -ForegroundColor Cyan

# Validar Admin
if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "Ejecutar como Administrador"
    exit 1
}

# Crear directorio
Write-Host "Instalando en: $InstallPath" -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path $InstallPath | Out-Null

# Copiar archivos (usando ruta relativa al script)
$SourceDir = Join-Path $PSScriptRoot "..\publish"
Get-ChildItem -Path $SourceDir | Copy-Item -Destination $InstallPath -Recurse -Force

# Crear Logs y dar permisos
$LogsDir = Join-Path $InstallPath "Logs"
New-Item -ItemType Directory -Force -Path $LogsDir | Out-Null

# Permisos para NETWORK SERVICE (cuenta predeterminada de servicios)
$acl = Get-Acl $InstallPath
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    "NT AUTHORITY\NETWORK SERVICE", 
    "Modify, ReadAndExecute, ListDirectory", 
    "ContainerInherit,ObjectInherit", 
    "None", 
    "Allow"
)
$acl.SetAccessRule($rule)
Set-Acl $InstallPath $acl

# Eliminar servicio existente si hay
if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
    Write-Host "Eliminando servicio anterior..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

# Crear servicio
Write-Host "Creando servicio Windows..." -ForegroundColor Yellow
$exePath = Join-Path $InstallPath "FichaCosto.Service.exe"
$binPath = "`"$exePath`" --environment Production"

sc.exe create $ServiceName binPath= $binPath start= auto DisplayName= "$DisplayName" | Out-Null
sc.exe description $ServiceName "$Description" | Out-Null

# Configurar recuperación: reiniciar en 1er y 2do fallo
sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/10000// | Out-Null

# Iniciar
Write-Host "Iniciando servicio..." -ForegroundColor Green
Start-Service -Name $ServiceName
Start-Sleep -Seconds 3

# Verificar
$svc = Get-Service -Name $ServiceName
if ($svc.Status -eq "Running") {
    Write-Host "✅ Servicio instalado y ejecutándose" -ForegroundColor Green
    Write-Host "   URL: http://localhost:$Port" -ForegroundColor Cyan
    Write-Host "   Logs: $LogsDir" -ForegroundColor Cyan
    
    # Test endpoint
    try {
        $resp = Invoke-RestMethod -Uri "http://localhost:$Port/api/health" -TimeoutSec 5
        Write-Host "   Health Check: $($resp.status)" -ForegroundColor Green
    } catch {
        Write-Warning "Servicio iniciado pero endpoint no responde aún"
    }
} else {
    Write-Error "❌ Fallo al iniciar. Estado: $($svc.Status)"
    exit 1
}
'@ | Out-File -FilePath "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\scripts\install-service.ps1" -Encoding utf8