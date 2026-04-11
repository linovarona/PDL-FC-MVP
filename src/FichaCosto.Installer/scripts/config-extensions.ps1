# Guardar esto en: src/FichaCosto.Installer/scripts/config-extensions.ps1
$script:WixExtensions = @{
    BasePath = "C:\Users\Yo\.wix\extensions\v4"
    Bal = "C:\Users\Yo\.wix\extensions\v4\wixtoolset.bal.wixext.4.0.6\wixext4\WixToolset.Bal.wixext.dll"
    Util = "C:\Users\Yo\.wix\extensions\v4\wixtoolset.util.wixext.4.0.6\wixext4\WixToolset.Util.wixext.dll"
    UI = "C:\Users\Yo\.wix\extensions\v4\wixtoolset.ui.wixext.4.0.6\wixext4\WixToolset.UI.wixext.dll"
    Firewall = "C:\Users\Yo\.wix\extensions\v4\wixtoolset.firewall.wixext.4.0.6\wixext4\WixToolset.Firewall.wixext.dll"
}

function Test-WixExtensions {
    Write-Host "=== VERIFICANDO EXTENSIONES WIX OFFLINE ===" -ForegroundColor Cyan
    $allOk = $true
    
    foreach ($ext in $WixExtensions.GetEnumerator()) {
        if ($ext.Key -eq 'BasePath') { continue }
        
        if (Test-Path $ext.Value) {
            Write-Host "✓ $($ext.Key): $(Split-Path $ext.Value -Leaf)" -ForegroundColor Green
        } else {
            Write-Host "âŒ $($ext.Key): No encontrado en $($ext.Value)" -ForegroundColor Red
            $allOk = $false
        }
    }
    
    if (-not $allOk) {
        throw "Faltan extensiones WiX. Verifica la ruta: $($WixExtensions.BasePath)"
    }
    
    return $WixExtensions
}