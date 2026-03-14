param(
    [string]$LocalRepo = "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\NuGetLocal\packages"
)

Write-Host "Restaurando paquetes en MODO OFFLINE..." -ForegroundColor Yellow
Write-Host "Fuente: $LocalRepo" -ForegroundColor Cyan

if (!(Test-Path $LocalRepo)) {
    Write-Host "ERROR: Repositorio local no encontrado" -ForegroundColor Red
    exit 1
}

$packageCount = (Get-ChildItem -Path $LocalRepo -Recurse -Filter "*.nupkg").Count
Write-Host "Paquetes disponibles localmente: $packageCount" -ForegroundColor Green

Write-Host "`nLimpiando caches..." -ForegroundColor Yellow
dotnet nuget locals all --clear

Write-Host "`nRestaurando..." -ForegroundColor Yellow
dotnet restore ..\FichaCosto.sln --source $LocalRepo --no-cache

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nRestore completado!" -ForegroundColor Green
} else {
    Write-Host "`nRestore fallo" -ForegroundColor Red
