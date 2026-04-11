# === CONFIGURACIÓN SEGÚN TU ENTORNO ===
$basePath = "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP"
$nugetLocal = "$basePath\NuGetLocal\packages"  # Tu estructura existente
$offlinePath = "$basePath\offline-installer"    # Para runtimes y extras

# Crear carpetas si no existen
New-Item -ItemType Directory -Force -Path $nugetLocal, "$offlinePath\runtimes", "$offlinePath\sql" | Out-Null

# 1. WiX EXTENSIONS (para v6.0.2 CLI - usan versiones 4.0.6 del SDK)
# NOTA: Con WiX CLI v6, las extensiones se instalan así:
$wixExtensions = @(
    "WixToolset.Bal.wixext/4.0.6",
    "WixToolset.Util.wixext/4.0.6",
    "WixToolset.UI.wixext/4.0.6",
    "WixToolset.Firewall.wixext/4.0.6"
)

foreach ($ext in $wixExtensions) {
    wix extension add -g $ext
    # Si estás offline, descargar manualmente desde:
    # https://www.nuget.org/packages/WixToolset.Bal.wixext/4.0.6
    # y guardar en $nugetLocal
}

# 2. NUGET PACKAGES (tu estructura)
$packages = @(
    "WixToolset.Sdk.4.0.6",  # SDK para build
    "System.Data.SQLite.Core.1.0.118"  # Para el seed de BD
)

foreach ($pkg in $packages) {
    nuget install $pkg -OutputDirectory $nugetLocal -Source https://api.nuget.org/v3/index.json
}

# 3. COPIAR ARCHIVOS DEL REPO (ya los tienes, solo verificar)
Copy-Item "$basePath\src\FichaCosto.Service\Data\SeedData.sql" "$offlinePath\sql\" -Force
Copy-Item "$basePath\src\FichaCosto.Service\Data\Schema.sql" "$offlinePath\sql\" -Force

Write-Host "✅ Recursos listos en tu estructura NuGetLocal"
Write-Host "   Extensiones WiX instaladas globalmente (v4.0.6)"
Write-Host "   Packages en: $nugetLocal"