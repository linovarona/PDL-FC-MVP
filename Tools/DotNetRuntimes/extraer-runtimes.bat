rem # En máquina CON internet: Extraer contenido del EXE (son autoextraíbles)
rem # Ejecutar esto en CMD (no PowerShell) para extraer sin instalar:
rem   aspnetcore-runtime-9.0.3-win-x64.exe /extract:D:\Temp\aspnetcore
rem   windowsdesktop-runtime-9.0.3-win-x64.exe /extract:D:\Temp\desktop  
   dotnet-runtime-9.0.3-win-x64.exe /extract:D:\Temp\dotnet

rem # Luego copiar las carpetas extraídas a tu máquina offline en:
rem # D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\Tools\RuntimesExtracted\