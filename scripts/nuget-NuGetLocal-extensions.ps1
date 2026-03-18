# Crear carpeta para extensiones
mkdir "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\NuGetLocal\extensions" -Force

# Descargar última versión
$version = "0.5.13"  # Verificar última versión en marketplace
$url = "https://marketplace.visualstudio.com/_apis/public/gallery/publishers/qwtel/vsextensions/sqlite-viewer/$version/vspackage"
$output = "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\NuGetLocal\extensions\sqlite-viewer-$version.vsix"

Invoke-WebRequest -Uri $url -OutFile $output -Headers @{"Accept"="application/octet-stream"}

# Verificar descarga
Get-Item $output | Select-Object Name, Length, LastWriteTime

cd "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\NuGetLocal\extensions"

# Instalar VSIX
& "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\VSIXInstaller.exe" /q sqlite-viewer-0.5.13.vsix

# Método 2: Manual desde VS 2022
# Menú Extensions → Manage Extensions
# Click en "Install from VSIX..." (abajo a la izquierda)
# Navegar a: D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\NuGetLocal\extensions\
# Seleccionar sqlite-viewer-X.X.X.vsix
# Reiniciar VS 2022