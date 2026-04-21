#GUIDs: Los GUIDs del ejemplo (A1B2C3D4...) 
#deben ser reemplazados por GUIDs únicos generados con uuidgen.exe o Visual Studio (Tools → Create GUID).


uuidgen.exe

# Generar 5 GUIDs para los componentes marcados como [GENERAR-NUEVO-GUID]
1..5 | ForEach-Object { [guid]::NewGuid().ToString().ToUpper() }