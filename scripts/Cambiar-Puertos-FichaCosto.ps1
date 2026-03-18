
# Crear el script de PowerShell para cambiar puertos de escucha

# Script: Cambiar-Puertos-FichaCosto.ps1
# Descripción: Cambia los puertos de escucha del servicio FichaCosto
# Ubicación: D:\\PrjSC#\\PDL\\FichaCosto\\Tools\\Scripts\\

param(
    [Parameter(Mandatory=$false)]
    [int]$PuertoHTTP = 5000,
    
    [Parameter(Mandatory=$false)]
    [int]$PuertoHTTPS = 5001,
    
    [Parameter(Mandatory=$false)]
    [switch]$SoloSwagger,
    
    [Parameter(Mandatory=$false)]
    [switch]$Verificar,
    
    [Parameter(Mandatory=$false)]
    [switch]$Ayuda
)

# Mostrar ayuda
if ($Ayuda) {
    @"
================================================================================
SCRIPT: Cambiar-Puertos-FichaCosto.ps1
================================================================================

USO:
    .\\Cambiar-Puertos-FichaCosto.ps1 [-PuertoHTTP <numero>] [-PuertoHTTPS <numero>] [opciones]

PARÁMETROS:
    -PuertoHTTP     Puerto para HTTP (default: 5000)
    -PuertoHTTPS    Puerto para HTTPS (default: 5001)
    -SoloSwagger    Solo cambiar puerto de Swagger/launchSettings
    -Verificar      Solo verificar configuración actual, no modificar
    -Ayuda          Mostrar esta ayuda

EJEMPLOS:
    # Cambiar puerto HTTP a 8080
    .\\Cambiar-Puertos-FichaCosto.ps1 -PuertoHTTP 8080
    
    # Cambiar ambos puertos
    .\\Cambiar-Puertos-FichaCosto.ps1 -PuertoHTTP 8080 -PuertoHTTPS 8443
    
    # Solo verificar configuración actual
    .\\Cambiar-Puertos-FichaCosto.ps1 -Verificar
    
    # Solo cambiar puerto de Swagger (desarrollo)
    .\\Cambiar-Puertos-FichaCosto.ps1 -PuertoHTTP 5002 -SoloSwagger

ARCHIVOS MODIFICADOS:
    - appsettings.json          (ApiSettings:Port)
    - appsettings.Development.json (urls, ApiSettings:Port)
    - Properties/launchSettings.json (applicationUrl)

================================================================================
"@ | Write-Host
    exit 0
}

# Configuración de rutas
$BasePath = "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\src\FichaCosto.Service"
$Archivos = @{
    AppSettings = Join-Path $BasePath "appsettings.json"
    AppSettingsDev = Join-Path $BasePath "appsettings.Development.json"
    LaunchSettings = Join-Path $BasePath "Properties\\launchSettings.json"
}

# Verificar que existe el proyecto
if (-not (Test-Path $BasePath)) {
    Write-Error "ERROR: No se encontró el proyecto en: $BasePath"
    Write-Host "Verifica la ruta del proyecto e intenta nuevamente." -ForegroundColor Yellow
    exit 1
}

Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "SCRIPT: Cambiar Puertos FichaCosto Service" -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host ""

# Función: Verificar puerto disponible
function Test-PuertoDisponible {
    param([int]$Puerto)
    
    try {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Any, $Puerto)
        $listener.Start()
        $listener.Stop()
        return $true
    } catch {
        return $false
    }
}

# Función: Verificar configuración actual
function Verificar-Configuracion {
    Write-Host "CONFIGURACIÓN ACTUAL:" -ForegroundColor Yellow
    Write-Host "------------------------" -ForegroundColor Yellow
    
    
    
    Write-Host "`n------------------------" -ForegroundColor Yellow
    Write-Host "PUERTOS EN USO ACTUAL:" -ForegroundColor Yellow
    
    $puertosComunes = @(5000, 5001, 8080, 8443, $PuertoHTTP, $PuertoHTTPS)
    $puertosUnicos = $puertosComunes | Select-Object -Unique
    
    foreach ($puerto in $puertosUnicos) {
        $disponible = Test-PuertoDisponible -Puerto $puerto
        $estado = if ($disponible) { "✅ DISPONIBLE" } else { "❌ EN USO" }
        $color = if ($disponible) { "Green" } else { "Red" }
        Write-Host "  Puerto $puerto : $estado" -ForegroundColor $color
    }
}

# Si solo se quiere verificar
if ($Verificar) {
    Verificar-Configuracion
    exit 0
}

# Verificar disponibilidad de nuevos puertos
Write-Host "VERIFICANDO NUEVOS PUERTOS..." -ForegroundColor Yellow
$httpDisponible = Test-PuertoDisponible -Puerto $PuertoHTTP
$httpsDisponible = Test-PuertoDisponible -Puerto $PuertoHTTPS

if (-not $httpDisponible) {
    Write-Warning "El puerto HTTP $PuertoHTTP está en uso por otro proceso."
    $continuar = Read-Host "¿Deseas continuar de todos modos? (S/N)"
    if ($continuar -ne 'S') { exit 1 }
}

if (-not $httpsDisponible) {
    Write-Warning "El puerto HTTPS $PuertoHTTPS está en uso por otro proceso."
    $continuar = Read-Host "¿Deseas continuar de todos modos? (S/N)"
    if ($continuar -ne 'S') { exit 1 }
}

Write-Host "Puertos verificados: HTTP=$PuertoHTTP, HTTPS=$PuertoHTTPS" -ForegroundColor Green
Write-Host ""

# Función: Actualizar appsettings.json
function Actualizar-AppSettings {
    param(
        [string]$Ruta,
        [int]$NuevoPuerto
    )
    
    if (-not (Test-Path $Ruta)) {
        Write-Warning "Archivo no encontrado: $Ruta"
        return $false
    }
    
    try {
        $json = Get-Content $Ruta -Raw | ConvertFrom-Json 
#-Depth 10
        
        # Navegar y modificar ApiSettings:Port
        if ($json.ApiSettings) {
            $json.ApiSettings.Port = $NuevoPuerto
            Write-Host "  ✓ ApiSettings.Port actualizado a $NuevoPuerto" -ForegroundColor Green
        } else {
            # Crear sección si no existe
            $json | Add-Member -NotePropertyName "ApiSettings" -NotePropertyValue @{
                Host = "localhost"
                Port = $NuevoPuerto
                BasePath = "/api"
            } -Force
            Write-Host "  ✓ Sección ApiSettings creada con Port=$NuevoPuerto" -ForegroundColor Green
        }
        
        # Guardar con formato
        $json | ConvertTo-Json -Depth 10 | Set-Content $Ruta -Encoding UTF8
        return $true
    } catch {
        Write-Error "Error actualizando $Ruta : $_"
        return $false
    }
}

# Función: Actualizar appsettings.Development.json (urls)
function Actualizar-AppSettingsDev {
    param(
        [string]$Ruta,
        [int]$PuertoHttp,
        [int]$PuertoHttps
    )
    
    if (-not (Test-Path $Ruta)) {
        Write-Warning "Archivo no encontrado: $Ruta"
        return $false
    }
    
    try {
        $json = Get-Content $Ruta -Raw | ConvertFrom-Json 
        #-De10pth 
        
        # Actualizar urls
        $urls = "http://localhost:$PuertoHttp;https://localhost:$PuertoHttps"
        $json.urls = $urls
        Write-Host "  ✓ urls actualizado a: $urls" -ForegroundColor Green
        
        # También actualizar ApiSettings:Port si existe
        if ($json.ApiSettings) {
            $json.ApiSettings.Port = $PuertoHttp
            Write-Host "  ✓ ApiSettings.Port actualizado a $PuertoHttp" -ForegroundColor Green
        }
        
        $json | ConvertTo-Json -Depth 10 | Set-Content $Ruta -Encoding UTF8
        return $true
    } catch {
        Write-Error "Error actualizando $Ruta : $_"
        return $false
    }
}

# Función: Actualizar launchSettings.json
function Actualizar-LaunchSettings {
    param(
        [string]$Ruta,
        [int]$PuertoHttp,
        [int]$PuertoHttps
    )
    
    if (-not (Test-Path $Ruta)) {
        Write-Warning "Archivo no encontrado: $Ruta"
        return $false
    }
    
    try {
        $json = Get-Content $Ruta -Raw | ConvertFrom-Json -Depth 10
        
        # Actualizar applicationUrl en profiles
        if ($json.profiles) {
            foreach ($profile in $json.profiles.PSObject.Properties) {
                $profileName = $profile.Name
                $profileValue = $profile.Value
                
                if ($profileValue.applicationUrl) {
                    $nuevaUrl = "http://localhost:$PuertoHttp;https://localhost:$PuertoHttps"
                    $profileValue.applicationUrl = $nuevaUrl
                    Write-Host "  ✓ profiles.$profileName.applicationUrl = $nuevaUrl" -ForegroundColor Green
                }
            }
        }
        
        $json | ConvertTo-Json -Depth 10 | Set-Content $Ruta -Encoding UTF8
        return $true
    } catch {
        Write-Error "Error actualizando $Ruta : $_"
        return $false
    }
}

# EJECUTAR ACTUALIZACIONES
Write-Host "ACTUALIZANDO ARCHIVOS DE CONFIGURACIÓN..." -ForegroundColor Yellow
Write-Host ""

$exitos = 0
$fallos = 0

# 1. appsettings.json (solo ApiSettings:Port, no urls)
if (-not $SoloSwagger) {
    Write-Host "[1/3] Actualizando appsettings.json..." -ForegroundColor Cyan
    $resultado = Actualizar-AppSettings -Ruta $Archivos.AppSettings -NuevoPuerto $PuertoHTTP
    if ($resultado) { $exitos++ } else { $fallos++ }
    Write-Host ""
}

# 2. appsettings.Development.json
Write-Host "[2/3] Actualizando appsettings.Development.json..." -ForegroundColor Cyan
$resultado = Actualizar-AppSettingsDev -Ruta $Archivos.AppSettingsDev -PuertoHttp $PuertoHTTP -PuertoHttps $PuertoHTTPS
if ($resultado) { $exitos++ } else { $fallos++ }
Write-Host ""

# 3. launchSettings.json (siempre actualizar, afecta Swagger)
Write-Host "[3/3] Actualizando launchSettings.json..." -ForegroundColor Cyan
$resultado = Actualizar-LaunchSettings -Ruta $Archivos.LaunchSettings -PuertoHttp $PuertoHTTP -PuertoHttps $PuertoHTTPS
if ($resultado) { $exitos++ } else { $fallos++ }
Write-Host ""

# RESUMEN
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "RESUMEN DE CAMBIOS" -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "Archivos actualizados exitosamente: $exitos" -ForegroundColor Green
if ($fallos -gt 0) {
    Write-Host "Archivos con errores: $fallos" -ForegroundColor Red
}
Write-Host ""
Write-Host "NUEVA CONFIGURACIÓN:" -ForegroundColor Yellow
Write-Host "  HTTP:  http://localhost:$PuertoHTTP" -ForegroundColor White
Write-Host "  HTTPS: https://localhost:$PuertoHTTPS" -ForegroundColor White
Write-Host "  Swagger: http://localhost:$PuertoHTTP/swagger" -ForegroundColor White
Write-Host ""

# Instrucciones post-cambio
Write-Host "INSTRUCCIONES:" -ForegroundColor Yellow
Write-Host "--------------" -ForegroundColor Yellow
Write-Host "1. Reinicia el servicio si está en ejecución:" -ForegroundColor White
Write-Host "   dotnet run --urls \"http://localhost:$PuertoHTTP\"" -ForegroundColor Gray
Write-Host ""
Write-Host "2. O si es Windows Service:" -ForegroundColor White
Write-Host "   Restart-Service -Name 'FichaCostoService'" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Verifica en navegador:" -ForegroundColor White
Write-Host "   http://localhost:$PuertoHTTP/swagger" -ForegroundColor Gray
Write-Host ""
Write-Host "================================================================================" -ForegroundColor Cyan

# Guardar configuración para referencia
$configGuardada = @{
    PuertoHTTP = $PuertoHTTP
    PuertoHTTPS = $PuertoHTTPS
    FechaCambio = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    ArchivosModificados = $exitos
} | ConvertTo-Json

$configPath = Join-Path $BasePath "..\\..\\puertos-config.json"
$configGuardada | Set-Content $configPath -Encoding UTF8
Write-Host "Configuración guardada en: puertos-config.json" -ForegroundColor DarkGray

exit $fallos

