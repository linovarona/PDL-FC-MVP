#Requires -RunAsAdministrator

param(
    [string]$ServiceName = "FichaCostoService",
    [string]$InstallPath = "C:\Program Files\FichaCostoService"
)

Write-Host "=== Desinstalador FichaCosto Service ===" -ForegroundColor Cyan

if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    sc.exe delete $ServiceName | Out-Null
    Write-Host "✅ Servicio eliminado" -ForegroundColor Green
}

if (Test-Path $InstallPath) {
    Remove-Item -Path $InstallPath -Recurse -Force
    Write-Host "✅ Archivos eliminados" -ForegroundColor Green
}

Write-Host "Desinstalación completada" -ForegroundColor Green
