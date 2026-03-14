$paquetes = @(
    # Core .NET 8.0
    @{ Id = "Microsoft.NETCore.App"; Version = "8.0.0" },
    
    # ASP.NET Core
    @{ Id = "Microsoft.AspNetCore.App"; Version = "8.0.0" },
    @{ Id = "Microsoft.AspNetCore.OpenApi"; Version = "8.0.0" },
    
    # SQLite y ORM
    @{ Id = "Microsoft.EntityFrameworkCore.Sqlite" },
    @{ Id = "Microsoft.EntityFrameworkCore" },
    @{ Id = "Microsoft.EntityFrameworkCore.Abstractions" },
    @{ Id = "Microsoft.EntityFrameworkCore.Analyzers" },
    @{ Id = "Microsoft.EntityFrameworkCore.Relational" },
    @{ Id = "Microsoft.Data.Sqlite" },
    @{ Id = "Microsoft.Data.Sqlite.Core" },
    @{ Id = "SQLitePCLRaw.bundle_e_sqlite3"; Version = "2.1.6" },
    @{ Id = "SQLitePCLRaw.core"; Version = "2.1.6" },
    @{ Id = "SQLitePCLRaw.lib.e_sqlite3"; Version = "2.1.6" },
    @{ Id = "SQLitePCLRaw.provider.e_sqlite3"; Version = "2.1.6" },
    @{ Id = "Dapper"; Version = "2.1.66" },
    @{ Id = "System.Data.SQLite"; Version = "2.0.2" },
    
    # Validacion
    @{ Id = "FluentValidation"; Version = "11.11.0" },
    @{ Id = "FluentValidation.AspNetCore"; Version = "11.3.1" },
    @{ Id = "FluentValidation.DependencyInjectionExtensions"; Version = "11.11.0" },
    
    # Logging
    @{ Id = "Serilog"; Version = "4.0.0" },
    @{ Id = "Serilog.AspNetCore"; Version = "8.0.3" },
    @{ Id = "Serilog.Extensions.Hosting"; Version = "8.0.0" },
    @{ Id = "Serilog.Extensions.Logging"; Version = "8.0.0" },
    @{ Id = "Serilog.Formatting.Compact"; Version = "3.0.0" },
    @{ Id = "Serilog.Settings.Configuration"; Version = "8.0.4" },
    @{ Id = "Serilog.Sinks.Console"; Version = "6.0.0" },
    @{ Id = "Serilog.Sinks.Debug"; Version = "3.0.0" },
    @{ Id = "Serilog.Sinks.File"; Version = "6.0.0" },
    
    # Windows Service
    @{ Id = "Microsoft.Extensions.Hosting.WindowsServices"; Version = "8.0.0" },
    @{ Id = "System.ServiceProcess.ServiceController"; Version = "8.0.0" },
    
    # Swagger
    @{ Id = "Swashbuckle.AspNetCore"; Version = "6.9.0" },
    @{ Id = "Swashbuckle.AspNetCore.Swagger"; Version = "6.9.0" },
    @{ Id = "Swashbuckle.AspNetCore.SwaggerGen"; Version = "6.9.0" },
    @{ Id = "Swashbuckle.AspNetCore.SwaggerUI"; Version = "6.9.0" },
    @{ Id = "Microsoft.OpenApi"; Version = "1.6.14" },
    
    # Excel
    @{ Id = "ClosedXML"; Version = "0.104.2" },
    @{ Id = "ClosedXML.Parser"; Version = "1.2.0" },
    @{ Id = "DocumentFormat.OpenXml"; Version = "3.1.1" },
    @{ Id = "DocumentFormat.OpenXml.Framework"; Version = "3.1.1" },
    @{ Id = "ExcelNumberFormat"; Version = "1.1.0" },
    @{ Id = "SixLabors.Fonts"; Version = "1.0.0" },
    @{ Id = "System.IO.Packaging"; Version = "8.0.0" },
    
    # Testing
    @{ Id = "xunit"; Version = "2.9.2" },
    @{ Id = "xunit.abstractions"; Version = "2.0.3" },
    @{ Id = "xunit.analyzers"; Version = "1.18.0" },
    @{ Id = "xunit.assert"; Version = "2.9.2" },
    @{ Id = "xunit.core"; Version = "2.9.2" },
    @{ Id = "xunit.extensibility.core"; Version = "2.9.2" },
    @{ Id = "xunit.extensibility.execution"; Version = "2.9.2" },
    @{ Id = "xunit.runner.visualstudio"; Version = "2.8.2" },
    @{ Id = "Microsoft.NET.Test.Sdk"; Version = "17.12.0" },
    @{ Id = "Moq"; Version = "4.20.72" },
    @{ Id = "Castle.Core"; Version = "5.1.1" },
    @{ Id = "Microsoft.AspNetCore.Mvc.Testing"; Version = "8.0.2" },
    @{ Id = "Microsoft.TestPlatform"; Version = "17.12.0" },
    @{ Id = "Microsoft.TestPlatform.ObjectModel"; Version = "17.12.0" },
    @{ Id = "Microsoft.TestPlatform.TestHost"; Version = "17.12.0" },
    @{ Id = "Newtonsoft.Json"; Version = "13.0.3" },
    @{ Id = "NuGet.Frameworks"; Version = "6.12.1" }
)

$nugetExe = "D:\PrjSC#\PDL\FichaCosto\Tools\nuget.exe"
$outputDir = "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP\NuGetLocal\packages"

if (!(Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

Write-Host "Descargando $($paquetes.Count) paquetes..." -ForegroundColor Green
Write-Host "Destino: $outputDir" -ForegroundColor Cyan
Write-Host ""

$exitosos = 0
$fallidos = 0

foreach ($pkg in $paquetes) {
    $id = $pkg.Id
    $version = $pkg.Version
    
    Write-Host "Descargando $id v$version..." -ForegroundColor Yellow -NoNewline
    
    & $nugetExe install $id -Version $version -OutputDirectory $outputDir -Source "https://api.nuget.org/v3/index.json" -NoHttpCache -DirectDownload -Verbosity quiet 2>$null
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host " [OK]" -ForegroundColor Green
        $exitosos++
    } else {
        Write-Host " [FALLO]" -ForegroundColor Red
        $fallidos++
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Descarga completada!" -ForegroundColor Green
Write-Host "Exitosos: $exitosos" -ForegroundColor Green
Write-Host "Fallidos: $fallidos" -ForegroundColor $(if($fallidos -gt 0){'Red'}else{'Green'})
Write-Host "Total: $($paquetes.Count)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan