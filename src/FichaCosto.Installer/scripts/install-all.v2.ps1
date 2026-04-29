#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Instalador completo FichaCosto MVP v0.6.2
#>
param(
    [string]$ProjectRoot = "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP"
)

$ErrorActionPreference = "Stop"
$ScriptsPath = Join-Path $ProjectRoot "src\FichaCosto.Installer\scripts"

$steps = @(
    @{ Name = "Verificación de Entorno"; Script = "verify-environment.ps1" },
    @{ Name = "Publicación del Servicio"; Script = "publisher.ps1" },
    @{ Name = "Compilación MSI"; Script = "compiler-msi.ps1" },
    @{ Name = "Compilación Bundle"; Script = "compiler-bundle.ps1" },
    @{ Name = "Pre-instalación"; Script = "pre-install.ps1" },
    @{ Name = "Instalación"; Script = "install.ps1" },
    @{ Name = "Post-instalación (Seed)"; Script = "post-install.ps1" }  # Nuevo paso
)

foreach ($step in $steps) {
    Write-Host "`n=== $($step.Name) ===" -ForegroundColor Cyan
    $scriptPath = Join-Path $ScriptsPath $step.Script
    
    if (-not (Test-Path $scriptPath)) {
        Write-Error "Script no encontrado: $scriptPath"
        exit 1
    }
    
    & $scriptPath
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Fallo en $($step.Name). Código: $LASTEXITCODE"
        exit $LASTEXITCODE
    }
}

Write-Host "`n=== INSTALACIÓN MVP v0.6.2 COMPLETADA ===" -ForegroundColor Green
Write-Host "Acceda a: http://localhost:5000/swagger" -ForegroundColor Yellow