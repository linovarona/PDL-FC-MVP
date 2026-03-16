<#
.SYNOPSIS
    Descarga paquetes NuGet necesarios para PDL-FC-MVP (Fase 3 y siguientes)
.DESCRIPTION
    Descarga todos los paquetes NuGet requeridos para desarrollo offline
    y los guarda en NuGetLocal\packages para instalación local.
.NOTES
    Ejecutar como Administrador no es necesario, pero requiere conexión a internet
    Ubicación: D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\scripts\
    Autor: Generado para proyecto PDL-FC-MVP
    Fecha: 2026-03-15
#>

[CmdletBinding()]
param()

# Configuración de rutas
$BasePath = "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP"
$PackagesPath = Join-Path $BasePath "NuGetLocal\packages"
$NuGetExePath = Join-Path $BasePath "NuGetLocal\nuget.exe"

# Crear carpetas si no existen
Write-Host "📁 Creando estructura de carpetas..." -ForegroundColor Cyan
New-Item -ItemType Directory -Path $PackagesPath -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $BasePath "NuGetLocal") -Force | Out-Null

Write-Host "📂 Paquetes se guardarán en: $PackagesPath" -ForegroundColor Yellow

# Descargar nuget.exe si no existe
if (-not (Test-Path $NuGetExePath)) {
    Write-Host "⬇️ Descargando nuget.exe..." -ForegroundColor Cyan
    try {
        Invoke-WebRequest -Uri "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile $NuGetExePath
        Write-Host "✅ nuget.exe descargado" -ForegroundColor Green
    }
    catch {
        Write-Error "❌ Falló descarga de nuget.exe: $_"
        exit 1
    }
}
else {
    Write-Host "✅ nuget.exe ya existe" -ForegroundColor Green
}

# Definir paquetes a descargar
$Paquetes = @(
    # Dapper y dependencias
    @{ Id = "Dapper"; Version = "2.1.66" },
    
    # SQLite
    @{ Id = "Microsoft.Data.Sqlite"; Version = "9.0.0" },
    @{ Id = "Microsoft.Data.Sqlite.Core"; Version = "9.0.0" },
    @{ Id = "SQLitePCLRaw.bundle_e_sqlite3"; Version = "2.1.10" },
    @{ Id = "SQLitePCLRaw.core"; Version = "2.1.10" },
    @{ Id = "SQLitePCLRaw.provider.e_sqlite3"; Version = "2.1.10" },
    @{ Id = "SQLitePCLRaw.lib.e_sqlite3"; Version = "2.1.10" },
    
    # Logging (Serilog - ya instalado pero por si acaso)
    @{ Id = "Serilog"; Version = "4.0.0" },
    @{ Id = "Serilog.AspNetCore"; Version = "9.0.0" },
    @{ Id = "Serilog.Sinks.File"; Version = "6.0.0" },
    @{ Id = "Serilog.Sinks.Console"; Version = "6.0.0" },
    
    # Testing
    @{ Id = "xunit"; Version = "2.9.2" },
    @{ Id = "xunit.runner.visualstudio"; Version = "3.0.0" },
    @{ Id = "Microsoft.NET.Test.Sdk"; Version = "17.12.0" },
    @{ Id = "coverlet.collector"; Version = "6.0.2" },
    
    # ASP.NET Core (por si acaso, para offline)
    @{ Id = "Microsoft.AspNetCore.OpenApi"; Version = "9.0.0" },
    @{ Id = "Swashbuckle.AspNetCore"; Version = "7.0.0" },
    
    # Extensiones comunes
    @{ Id = "Microsoft.Extensions.Configuration"; Version = "9.0.0" },
    @{ Id = "Microsoft.Extensions.DependencyInjection"; Version = "9.0.0" },
    @{ Id = "Microsoft.Extensions.Logging"; Version = "9.0.0" }
)

Write-Host "`n📦 Iniciando descarga de $($Paquetes.Count) paquetes..." -ForegroundColor Cyan

$Exitosos = 0
$Fallidos = 0

foreach ($pkg in $Paquetes) {
    $id = $pkg.Id
    $version = $pkg.Version
    $outputFile = Join-Path $PackagesPath "$id.$version.nupkg"
    
    Write-Host "⬇️ Descargando $id v$version..." -NoNewline
    
    try {
        & $NuGetExePath install $id -Version $version -OutputDirectory $PackagesPath -PackageSaveMode nupkg -Verbosity quiet -NonInteractive
        
        if (Test-Path $outputFile) {
            Write-Host " ✅" -ForegroundColor Green
            $Exitosos++
        }
        else {
            # Intentar buscar en subcarpetas que crea nuget.exe
            $found = Get-ChildItem -Path $PackagesPath -Filter "$id.$version.nupkg" -Recurse | Select-Object -First 1
            if ($found) {
                Write-Host " ✅ (en subcarpeta)" -ForegroundColor Green
                $Exitosos++
            }
            else {
                Write-Host " ⚠️ No verificado" -ForegroundColor Yellow
            }
        }
    }
    catch {
        Write-Host " ❌ Error: $_" -ForegroundColor Red
        $Fallidos++
    }
}

# Resumen
Write-Host "`n" + ("="*50) -ForegroundColor Cyan
Write-Host "📊 RESUMEN DE DESCARGA" -ForegroundColor Cyan
Write-Host ("="*50) -ForegroundColor Cyan
Write-Host "✅ Exitosos: $Exitosos" -ForegroundColor Green
Write-Host "❌ Fallidos: $Fallidos" -ForegroundColor Red
Write-Host "📂 Ubicación: $PackagesPath" -ForegroundColor Yellow

# Listar paquetes descargados
Write-Host "`n📋 Paquetes disponibles:" -ForegroundColor Cyan
Get-ChildItem -Path $PackagesPath -Filter "*.nupkg" | 
    Select-Object Name, @{N="Size(MB)";E={[math]::Round($_.Length/1MB,2)}} |
    Format-Table -AutoSize

# Crear archivo de configuración nuget.config para uso offline
$NuGetConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="LocalPackages" value="$PackagesPath" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="LocalPackages">
      <pattern>*</pattern>
    </packageSource>
  </packageSourceMapping>
</configuration>
"@

$NuGetConfigPath = Join-Path $BasePath "NuGetLocal\nuget.config"
$NuGetConfig | Out-File -FilePath $NuGetConfigPath -Encoding UTF8

Write-Host "`n📝 Configuración local creada: $NuGetConfigPath" -ForegroundColor Green
Write-Host "`n💡 Para usar paquetes offline en el proyecto:" -ForegroundColor Cyan
Write-Host "   dotnet restore --source $PackagesPath" -ForegroundColor White
Write-Host "   # o copiar nuget.config a la raíz del proyecto" -ForegroundColor Gray

Write-Host "`n✨ Script completado" -ForegroundColor Green