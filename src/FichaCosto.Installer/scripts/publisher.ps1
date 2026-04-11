# Desde la raíz del proyecto
cd "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP"

# Publicar en modo Release, self-contained
dotnet publish src\FichaCosto.Service\FichaCosto.Service.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -o "publish" `
    --source "NuGetLocal\runtimes" `
    --source "NuGetLocal\packages"