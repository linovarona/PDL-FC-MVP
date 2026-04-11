$paquetes = @(
    
    
    @{ Id = "WixToolset.Sdk"; Version = "4.0.6" },
    @{ Id = "System.Data.SQLite.Core"; Version = "1.0.118" }



)

$nugetExe = "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\Tools\NuGetLocal\nuget.exe"
$outputDir = "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\NuGetLocal\packages"

if (!(Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

Write-Host "Descargando $($paquetes.Count) paquetes..." -ForegroundColor Green
Write-Host "Destino: $outputDir" -ForegroundColor Cyan
Write-Host ""

$exitosos = 0
$fallidos = 0

foreach ($pkg in $paquetes) {
    $id = $pkg.Id
    $version = $pkg.Version
    
    Write-Host "Descargando $id v$version..." -ForegroundColor Yellow -NoNewline
    
     nuget install $id -Version $version -OutputDirectory $outputDir -Source "https://api.nuget.org/v3/index.json" -NoHttpCache -DirectDownload -Verbosity quiet 2>$null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host " [OK]" -ForegroundColor Green
        $exitosos++
    } else {
        Write-Host " [FALLO]" -ForegroundColor Red
        $fallidos++
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Descarga completada!" -ForegroundColor Green
Write-Host "Exitosos: $exitosos" -ForegroundColor Green
Write-Host "Fallidos: $fallidos" -ForegroundColor $(if($fallidos -gt 0){'Red'}else{'Green'})
Write-Host "Total: $($paquetes.Count)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan