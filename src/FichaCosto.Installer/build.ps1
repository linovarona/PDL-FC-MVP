param(
    [string]$Configuration = "Release",
    [string]$Version = "0.6.2"
)

$ErrorActionPreference = "Stop"
$basePath = Split-Path $PSScriptRoot -Parent
$installerPath = $PSScriptRoot
$publishPath = "$basePath\FichaCosto.Service\bin\$Configuration\net9.0\win-x64\publish"

Write-Host "=== BUILD INSTALLER v$Version (WiX 4.0.6, x64) ===" -ForegroundColor Cyan

# Publicar x64
Write-Host "`n[1/4] Publicando servicio (x64)..." -ForegroundColor Yellow
dotnet publish "$basePath\FichaCosto.Service\FichaCosto.Service.csproj" `
    -c $Configuration `
    -r win-x64 `
    --self-contained false `
    -o $publishPath

# Copiar SQL files
Write-Host "`n[2/4] Copiando archivos de datos..." -ForegroundColor Yellow
$dataPath = "$publishPath\Data"
if (!(Test-Path $dataPath)) { New-Item -ItemType Directory -Path $dataPath -Force | Out-Null }
Copy-Item "$basePath\FichaCosto.Service\Data\Schema.sql" $dataPath -Force
Copy-Item "$basePath\FichaCosto.Service\Data\SeedData.sql" $dataPath -Force

# Build MSI x64
Write-Host "`n[3/4] Compilando MSI x64..." -ForegroundColor Yellow
wix build "$installerPath\Package.wxs" `
    -o "$installerPath\FichaCostoService-Setup-v$Version.msi" `
    -arch x64 `
    -d PublishDir=$publishPath `
    -ext WixToolset.Util.wixext `
    -ext WixToolset.Firewall.wixext

# Build Bundle
Write-Host "`n[4/4] Compilando Bundle..." -ForegroundColor Yellow
wix build "$installerPath\Bundle.wxs" `
    -o "$installerPath\FichaCostoService-Bundle-v$Version.exe" `
    -arch x64 `
    -ext WixToolset.Bal.wixext `
    -ext WixToolset.Util.wixext

Write-Host "`n✅ BUILD COMPLETADO" -ForegroundColor Green
Get-Item "$installerPath\FichaCostoService-Setup-v$Version.msi", "$installerPath\FichaCostoService-Bundle-v$Version.exe" -ErrorAction SilentlyContinue | 
    Select Name, @{N="SizeMB";E={[math]::Round($_.Length/1MB,2)}}