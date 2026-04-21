cd "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\src\FichaCosto.Installer"

# Limpieza de builds anteriores
if (Test-Path "bin") { Remove-Item -Recurse -Force "bin" }
if (Test-Path "obj") { Remove-Item -Recurse -Force "obj" }

# Compilar con WiX v4 (offline)
wix build -arch x64 -ext "C:\Users\Yo\.wix\extensions\v4\wixtoolset.util.wixext.4.0.6\wixext4\WixToolset.Util.wixext.dll" `
  -ext "C:\Users\Yo\.wix\extensions\v4\wixtoolset.ui.wixext.4.0.6\wixext4\WixToolset.UI.wixext.dll" `
  -out "bin\FichaCosto_0.6.2_x64.msi" `
  Package.wxs