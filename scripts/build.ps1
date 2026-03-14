param(
    [string]$Configuration = "Release",
    [string]$SolutionPath = "..\FichaCosto.sln"
)

$msbuildPath = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"

# Si tienes Professional o Enterprise, cambiar la ruta:
# $msbuildPath = "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
# $msbuildPath = "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"

if (!(Test-Path $msbuildPath)) {
    Write-Host "MSBuild no encontrado en: $msbuildPath" -ForegroundColor Red
    Write-Host "Buscando MSBuild..." -ForegroundColor Yellow
    
    $msbuildPath = Get-ChildItem -Path "C:\Program Files\Microsoft Visual Studio\2022\" -Recurse -Filter "MSBuild.exe" | Select-Object -First 1 -ExpandProperty FullName
    
    if (!$msbuildPath) {
        Write-Host "MSBuild no encontrado. Instalar VS 2022 con carga de trabajo 'Desarrollo de ASP.NET'" -ForegroundColor Red
        exit 1
    }
}

Write-Host "Usando MSBuild: $msbuildPath" -ForegroundColor Cyan
Write-Host "Compilando solucion ($Configuration)..." -ForegroundColor Green

& $msbuildPath $SolutionPath /p:Configuration=$Configuration /p:Platform="Any CPU" /verbosity:minimal /restore

if ($LASTEXITCODE -eq 0) {
    Write-Host "Build exitoso!" -ForegroundColor Green
} else {
    Write-Host "Build fallo con codigo: $LASTEXITCODE" -ForegroundColor Red
}