Get-TimeZone
# Debe mostrar: (UTC-05:00) Bogotá, Lima, Quito, Rio Branco

# Detener con Ctrl+C, luego:
dotnet run

# 1. ¿El servicio está corriendo?
Get-Process -Name "FichaCosto.Service" -ErrorAction SilentlyContinue | Select-Object Id, ProcessName

# 2. ¿Qué puerto está escuchando?
Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue

# 3. ¿Responde a ping local?
Test-NetConnection -ComputerName localhost -Port 5000

# Verificar reglas
Get-NetFirewallRule -DisplayName "*5000*" -ErrorAction SilentlyContinue

# Agregar excepción temporal (como Administrador)
New-NetFirewallRule -DisplayName "FichaCosto Dev" -Direction Inbound -LocalPort 5000 -Protocol TCP -Action Allow

# Verificar qué proceso usa el puerto 5000
netstat -ano | findstr :5000

# O con PowerShell moderno:
Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue | 
    Select-Object LocalAddress, LocalPort, State, OwningProcess

# 1. Verificar qué puerto usa ahora
Get-NetTCPConnection -LocalPort 5001 -ErrorAction SilentlyContinue

# 2. Probar en navegador
start http://localhost:5000/swagger

# Buscar archivos relacionados con DatabaseInitializer
Get-ChildItem -Path "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\src\FichaCosto.Service" -Recurse -Filter "*Database*" | Select-Object FullName

# Buscar en el contenido de archivos
Select-String -Path "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\src\FichaCosto.Service\*.cs" -Pattern "DatabaseInitializer|DatabaseInitializationService"

Select-String -Path "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\tests\FichaCosto.Service.Tests\RepositorySharedTests.cs" -Pattern "new ClienteRepository|new ProductoRepository|new FichaRepository" -Context 0,2