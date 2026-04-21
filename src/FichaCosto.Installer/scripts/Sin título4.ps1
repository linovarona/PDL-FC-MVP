# 1. Verificar que AdminController.cs existe y tiene contenido
Get-Content "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\src\FichaCosto.Service\Controllers\AdminController.cs" | Select-Object -First 20

# 2. Compilar y ver si hay errores
cd "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\src\FichaCosto.Service"
dotnet build --verbosity normal 2>&1 | findstr "AdminController"

# 3. Verificar que el DLL contiene el controller
dotnet run #&
Start-Sleep 5
$controllers = [System.Reflection.Assembly]::LoadFrom(".\bin\Debug\net9.0\FichaCosto.Service.dll").GetTypes() | Where-Object { $_.Name -like "*Controller*" }
$controllers | Select-Object FullName