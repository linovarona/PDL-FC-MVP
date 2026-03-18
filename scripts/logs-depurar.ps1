# Verificar dónde está creando los logs realmente
Get-Process -Name "FichaCosto.Service" | Select-Object Path

# Listar todos los archivos log del proceso
Get-ChildItem -Path "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP" -Recurse -Filter "log-*.txt" | 
    Select-Object FullName, LastWriteTime, Length | 
    Sort-Object LastWriteTime -Descending

# Crear carpeta Logs manualmente con permisos
$logPath = "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\src\FichaCosto.Service\Logs"
New-Item -ItemType Directory -Path $logPath -Force

# Verificar permisos
Get-Acl $logPath | Select-Object -ExpandProperty Access | Select-Object IdentityReference, FileSystemRights

#Para usar paquetes offline en el proyecto:
   dotnet restore --source D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\NuGetLocal\packages