param(
    [string]$Configuration = "Release",
    [string]$OutputPath = ".\publish"
)

Write-Host "Publishing FichaCosto.Service..." -ForegroundColor Green

dotnet publish src\FichaCosto.Service `
    --configuration $Configuration `
    --output $OutputPath `
    --self-contained false `
    /p:PublishSingleFile=false

if ($LASTEXITCODE -eq 0) {
    Write-Host "Publish successful! Output: $OutputPath" -ForegroundColor Green
} else {
    Write-Host "Publish failed!" -ForegroundColor Red
}