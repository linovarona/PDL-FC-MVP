# 1. Verificar que AdminController.cs está incluido en el proyecto
cd "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\src\FichaCosto.Service"
Get-ChildItem -Recurse -Filter "AdminController.cs"

# 2. Verificar el csproj
Get-Content "FichaCosto.Service.csproj"

# 3. Publicar y verificar que AdminController.cs se copia
dotnet publish -c Release -r win-x64 --self-contained false -o "C:\temp\publish-test"
Get-ChildItem "C:\temp\publish-test" -Recurse -Filter "*Admin*"