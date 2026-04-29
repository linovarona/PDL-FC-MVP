# 1. Detener servicio
Stop-Service FichaCostoService -ErrorAction SilentlyContinue

# 2. Limpiar todo
cd "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP"
Remove-Item -Recurse -Force "src\FichaCosto.Service\bin" -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force "src\FichaCosto.Service\obj" -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force "src\FichaCosto.Installer\bin" -ErrorAction SilentlyContinue

# 3. Verificar AdminController existe
Get-Content "src\FichaCosto.Service\Controllers\AdminController.cs" | Select-Object -First 5

# 4. Recompilar todo
dotnet build src\FichaCosto.Service\FichaCosto.Service.csproj -c Release

# 5. Verificar que el DLL contiene AdminController
$dllPath = "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\src\FichaCosto.Service\bin\Release\net9.0\FichaCosto.Service.dll"
[Reflection.Assembly]::LoadFrom($dllPath).GetTypes() | Where-Object { $_.Name -like "*Admin*" }

