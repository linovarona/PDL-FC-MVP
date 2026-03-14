Write-Host "Ejecutando tests con dotnet test..." -ForegroundColor Green

dotnet test ..\FichaCosto.sln --verbosity normal --no-build

if ($LASTEXITCODE -eq 0) {
    Write-Host "Todos los tests pasaron!" -ForegroundColor Green
} else {
    Write-Host "Algunos tests fallaron!" -ForegroundColor Red
}