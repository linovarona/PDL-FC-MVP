Kimi: Entendido. Me equivoqué al asumir versiones y no respeté tu estructura de carpetas existente. Aquí la corrección y una propuesta para evitar esto en el futuro.

---

## 🔧 Corrección Inmediata: WiX Versiones

**Situación Real:**
- Tienes **WiX CLI Tool v6.0.2** (la última versión del ejecutable `wix.exe`)
- Este CLI es compatible con **WiX SDK v4.0.x** (el formato de los archivos `.wxs`)
- No necesitas descargar nada de WiX, ya lo tienes.

**Analogía:**
> Es como tener `dotnet.exe` v8.0 pero compilar proyectos para .NET 6. El tool es v6, pero el "lenguaje" es v4.

---

## 📋 Formato Estándar para Informar Contexto (Template)

Al inicio de cada sesión (especialmente si trabajo offline), copia y completa esto:

```markdown
### 🖥️ CONTEXTO SESIÓN [Fecha]

**Estado Offline/Online:** [Offline | Online ahora pero luego offline]
**Fase Actual:** [Ej: 6.5 - Instalador WiX]
**Tarea Específica:** [Ej: Corregir permisos Logs + Poblar BD]

**Herramientas Instaladas:**
- WiX: v6.0.2 (CLI) / SDK v4.0.x
- .NET: [Ej: 9.0.3]
- PowerShell: [Ej: 5.1 / 7.4]

**Estructura de Rutas:**
- Base proyecto: `D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP`
- NuGet Local: `[ruta]\NuGetLocal\packages`
- Descargas offline: `[ruta]\offline`
- WiX extensions: `[ruta]\wix-ext`

**Versiones Producto:**
- Bundle: v[X.X.X]
- MSI: v[X.X.X]
- Servicio: v[X.X.X]

**Problemas Conocidos Actuales:**
- [ ] Logs sin permisos
- [ ] BD vacía (no ejecuta SeedData.sql)
- [ ] Versión desfasada
- [ ] Otro: ___________

**Recursos ya descargados:**
- [ ] Runtimes .NET
- [ ] Paquetes NuGet específicos: ___________
- [ ] WiX extensions: ___________
```

---

## 📦 SECCIÓN 1 CORREGIDA: Descarga (Adaptada a tu estructura)

Usando **tu ruta real** y **WiX v6.0.2 ya instalado**:

```powershell
# === CONFIGURACIÓN SEGÚN TU ENTORNO ===
$basePath = "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP"
$nugetLocal = "$basePath\NuGetLocal\packages"  # Tu estructura existente
$offlinePath = "$basePath\offline-installer"    # Para runtimes y extras

# Crear carpetas si no existen
New-Item -ItemType Directory -Force -Path $nugetLocal, "$offlinePath\runtimes", "$offlinePath\sql" | Out-Null

# 1. WiX EXTENSIONS (para v6.0.2 CLI - usan versiones 4.0.6 del SDK)
# NOTA: Con WiX CLI v6, las extensiones se instalan así:
$wixExtensions = @(
    "WixToolset.Bal.wixext/4.0.6",
    "WixToolset.Util.wixext/4.0.6",
    "WixToolset.UI.wixext/4.0.6",
    "WixToolset.Firewall.wixext/4.0.6"
)

foreach ($ext in $wixExtensions) {
    wix extension add -g $ext
    # Si estás offline, descargar manualmente desde:
    # https://www.nuget.org/packages/WixToolset.Bal.wixext/4.0.6
    # y guardar en $nugetLocal
}

# 2. NUGET PACKAGES (tu estructura)
$packages = @(
    "WixToolset.Sdk.4.0.6",  # SDK para build
    "System.Data.SQLite.Core.1.0.118"  # Para el seed de BD
)

foreach ($pkg in $packages) {
    nuget install $pkg -OutputDirectory $nugetLocal -Source https://api.nuget.org/v3/index.json
}

# 3. COPIAR ARCHIVOS DEL REPO (ya los tienes, solo verificar)
Copy-Item "$basePath\src\FichaCosto.Service\Data\SeedData.sql" "$offlinePath\sql\" -Force
Copy-Item "$basePath\src\FichaCosto.Service\Data\Schema.sql" "$offlinePath\sql\" -Force

Write-Host "✅ Recursos listos en tu estructura NuGetLocal"
Write-Host "   Extensiones WiX instaladas globalmente (v4.0.6)"
Write-Host "   Packages en: $nugetLocal"
```

---

## 🎯 Procedimiento Corregido (WiX v6.0.2 + tu estructura)

**Nota importante sobre WiX v6.0.2 vs v4.0.6:**
- Los archivos `.wxs` **no cambian** (siguen siendo sintaxis v4)
- Los comandos `wix build` **no cambian**
- Lo único que cambia es la instalación de extensiones (comando arriba)

### Archivo: `Package.wxs` (Versión corregida x64)

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs"
     xmlns:util="http://wixtoolset.org/schemas/v4/wxs/util"
     xmlns:fire="http://wixtoolset.org/schemas/v4/wxs/firewall">

  <Package Name="FichaCosto Service MVP"
           Language="1033"
           Version="0.6.2"
           Manufacturer="PyMEs Dev Solutions"
           UpgradeCode="f8f3bd79-de8c-45a0-96a3-576edd3ab121"
           Scope="perMachine">
    
    <!-- CORRECCIÓN: x64 nativo -->
    <MajorUpgrade DowngradeErrorMessage="Una versión posterior ya está instalada." />
    
    <MediaTemplate EmbedCab="yes" />

    <Feature Id="MainFeature" Title="FichaCosto Service" Level="1">
      <ComponentGroupRef Id="ServiceComponents" />
      <ComponentGroupRef Id="DatabaseComponents" />
      <ComponentRef Id="SeedDataComponent" />
      <ComponentRef Id="FirewallRule" />
    </Feature>

    <!-- CORRECCIÓN: ProgramFiles64Folder para x64 -->
    <StandardDirectory Id="ProgramFiles64Folder">
      <Directory Id="INSTALLFOLDER" Name="FichaCostoService">
        <Directory Id="DATAFOLDER" Name="Data" />
        <Directory Id="LOGSFOLDER" Name="Logs" />
      </Directory>
    </StandardDirectory>

    <!-- Componentes del Servicio -->
    <ComponentGroup Id="ServiceComponents" Directory="INSTALLFOLDER">
      <Component>
        <File Source="$(var.PublishDir)\FichaCosto.Service.exe" KeyPath="yes" />
        <ServiceInstall Id="FichaCostoService"
                        Name="FichaCostoService"
                        DisplayName="FichaCosto Service MVP"
                        Description="Servicio de automatización de fichas de costo para PyMEs"
                        Start="auto"
                        Type="ownProcess"
                        ErrorControl="normal"
                        Account="NT AUTHORITY\LocalService" />
        <ServiceControl Id="StartService" 
                      Name="FichaCostoService" 
                      Start="install" 
                      Stop="both" 
                      Remove="uninstall" />
      </Component>
      
      <!-- Agregar todas las DLLs necesarias -->
      <Component><File Source="$(var.PublishDir)\FichaCosto.Service.dll" /></Component>
      <Component><File Source="$(var.PublishDir)\FichaCosto.Core.dll" /></Component>
      <Component><File Source="$(var.PublishDir)\FichaCosto.Infrastructure.dll" /></Component>
      <Component><File Source="$(var.PublishDir)\System.Data.SQLite.dll" /></Component>
    </ComponentGroup>

    <!-- Componentes de Base de Datos -->
    <ComponentGroup Id="DatabaseComponents" Directory="DATAFOLDER">
      <Component>
        <File Source="$(var.PublishDir)\Data\Schema.sql" Name="Schema.sql" />
      </Component>
      <Component>
        <File Source="$(var.PublishDir)\Data\SeedData.sql" Name="SeedData.sql" />
      </Component>
    </ComponentGroup>

    <!-- Componente: Seed de Datos vía PowerShell CustomAction -->
    <Component Id="SeedDataComponent" Directory="INSTALLFOLDER">
      <File Source="SeedDatabase.ps1" Name="SeedDatabase.ps1" />
    </Component>

    <!-- CORRECCIÓN: Permisos de Logs para SYSTEM (LocalService corre como SYSTEM) -->
    <Component Id="LogsPermissions" Directory="LOGSFOLDER">
      <CreateFolder />
      <!-- SYSTEM tiene SID S-1-5-18 -->
      <util:PermissionEx Sddl="D:P(A;OICI;GA;;;SY)(A;OICI;GA;;;BA)" />
      <!-- Explicación: 
           SY = SYSTEM (S-1-5-18) 
           BA = Built-in Administrators
           GA = Generic All
           OICI = Object Inherit + Container Inherit
      -->
    </Component>

    <!-- Firewall -->
    <Component Id="FirewallRule" Directory="INSTALLFOLDER">
      <fire:FirewallRule Id="FichaCostoPort5000"
                         Name="FichaCosto Service (Port 5000)"
                         Port="5000"
                         Protocol="tcp"
                         Scope="localSubnet"
                         Profile="all" />
    </Component>

    <!-- Custom Action para ejecutar Seed -->
    <CustomAction Id="RunSeedScript" 
                  BinaryRef="Wix4UtilCA$(var.ProcessorArchitecture)"
                  DllEntry="WixQuietExec64"
                  Execute="deferred"
                  Return="check"
                  Impersonate="no" />
    
    <InstallExecuteSequence>
      <Custom Action="RunSeedScript" After="InstallFiles">
        <![CDATA[NOT Installed AND NOT REMOVE]]>
      </Custom>
    </InstallExecuteSequence>

  </Package>
</Wix>
```

### Archivo: `SeedDatabase.ps1` (Simplificado y robusto)

```powershell
param(
    [Parameter(Mandatory=$true)]
    [string]$InstallDir
)

$ErrorActionPreference = "Stop"
$logFile = "$env:TEMP\FichaCosto-Seed-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"

function Write-Log {
    param([string]$Message)
    "$(Get-Date -Format 'HH:mm:ss') - $Message" | Tee-Object -FilePath $logFile -Append | Write-Host
}

try {
    Write-Log "=== SEED DATABASE v0.6.2 ==="
    
    $dataDir = Join-Path $InstallDir "Data"
    $dbPath = Join-Path $dataDir "fichacosto.db"
    $schemaFile = Join-Path $dataDir "Schema.sql"
    $seedFile = Join-Path $dataDir "SeedData.sql"
    
    Write-Log "DB Path: $dbPath"
    
    # Verificar archivos SQL existen
    if (-not (Test-Path $schemaFile)) { throw "Schema.sql no encontrado en $dataDir" }
    if (-not (Test-Path $seedFile)) { throw "SeedData.sql no encontrado en $dataDir" }
    
    # Buscar System.Data.SQLite.dll (debe estar en INSTALLFOLDER)
    $sqliteDll = Join-Path $InstallDir "System.Data.SQLite.dll"
    if (-not (Test-Path $sqliteDll)) {
        throw "System.Data.SQLite.dll no encontrado en $InstallDir"
    }
    
    Add-Type -Path $sqliteDll
    
    # Crear conexión (crea el archivo si no existe)
    $connStr = "Data Source=$dbPath;Version=3;New=True;"
    $conn = New-Object System.Data.SQLite.SQLiteConnection($connStr)
    $conn.Open()
    Write-Log "Conexión abierta"
    
    # Leer y ejecutar Schema
    $schema = Get-Content $schemaFile -Raw -Encoding UTF8
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $schema
    $cmd.ExecuteNonQuery() | Out-Null
    Write-Log "Schema ejecutado"
    
    # Leer y ejecutar SeedData
    $seed = Get-Content $seedFile -Raw -Encoding UTF8
    $cmd.CommandText = $seed
    $cmd.ExecuteNonQuery() | Out-Null
    Write-Log "SeedData ejecutado"
    
    # Verificación
    $cmd.CommandText = "SELECT COUNT(*) FROM Clientes"
    $count = $cmd.ExecuteScalar()
    Write-Log "VERIFICACIÓN: $count clientes en BD"
    
    $conn.Close()
    Write-Log "=== SEED COMPLETADO ==="
    
    exit 0
}
catch {
    Write-Log "ERROR: $_"
    Write-Log $_.ScriptStackTrace
    exit 1
}
```

### Archivo: `Bundle.wxs` (Versión sincronizada v0.6.2)

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs"
     xmlns:bal="http://wixtoolset.org/schemas/v4/wxs/bal"
     xmlns:util="http://wixtoolset.org/schemas/v4/wxs/util">

  <Bundle Name="FichaCosto Service MVP Bundle"
          Version="0.6.2"
          Manufacturer="PyMEs Dev Solutions"
          UpgradeCode="a614d4d1-997c-420a-8f44-7cb79f93af84">

    <BootstrapperApplication>
      <bal:WixStandardBootstrapperApplication 
        Theme="rtfLargeLicense"
        LicenseFile="license.rtf"
        ShowVersion="yes" />
    </BootstrapperApplication>

    <util:RegistrySearch Id="NetRuntimeInstalled"
                         Root="HKLM"
                         Key="SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.NETCore.App"
                         Value="9.0.3"
                         Result="exists"
                         Variable="NetRuntimeInstalled" />
                         
    <util:RegistrySearch Id="AspNetRuntimeInstalled"
                         Root="HKLM"
                         Key="SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.AspNetCore.App"
                         Value="9.0.3"
                         Result="exists"
                         Variable="AspNetRuntimeInstalled" />

    <util:RegistrySearch Id="DesktopRuntimeInstalled"
                         Root="HKLM"
                         Key="SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App"
                         Value="9.0.3"
                         Result="exists"
                         Variable="DesktopRuntimeInstalled" />

    <Chain>
      <ExePackage Id="NetRuntime" 
                  DisplayName="Microsoft .NET 9.0 Runtime (x64)"
                  SourceFile="dotnet-runtime-9.0.3-win-x64.exe"
                  InstallArguments="/quiet /norestart"
                  DetectCondition="NetRuntimeInstalled"
                  Permanent="yes" />

      <ExePackage Id="AspNetRuntime"
                  DisplayName="Microsoft ASP.NET Core 9.0 Runtime (x64)"
                  SourceFile="aspnetcore-runtime-9.0.3-win-x64.exe"
                  InstallArguments="/quiet /norestart"
                  DetectCondition="AspNetRuntimeInstalled"
                  Permanent="yes" />

      <ExePackage Id="DesktopRuntime"
                  DisplayName="Microsoft Windows Desktop Runtime 9.0 (x64)"
                  SourceFile="windowsdesktop-runtime-9.0.3-win-x64.exe"
                  InstallArguments="/quiet /norestart"
                  DetectCondition="DesktopRuntimeInstalled"
                  Permanent="yes" />

      <!-- MSI v0.6.2 -->
      <MsiPackage Id="FichaCostoService"
                  DisplayName="FichaCosto Service MVP v0.6.2"
                  SourceFile="FichaCostoService-Setup-v0.6.2.msi"
                  Vital="yes" />
    </Chain>
  </Bundle>
</Wix>
```

---

## 🔨 Build Script (adaptado a WiX v6.0.2)

```powershell
param(
    [string]$Configuration = "Release",
    [string]$Version = "0.6.2"
)

$ErrorActionPreference = "Stop"
$basePath = "D:\PrjSC#\PDL\FichaCosto\PDL-FC-MVP"
$installerPath = "$basePath\src\FichaCosto.Installer"
$publishPath = "$basePath\src\FichaCosto.Service\bin\$Configuration\net9.0\win-x64\publish"

Write-Host "=== BUILD INSTALLER v$Version (WiX CLI v6.0.2) ===" -ForegroundColor Cyan

# 1. Publicar servicio x64
Write-Host "`n[1/5] Publicando servicio (x64)..." -ForegroundColor Yellow
dotnet publish "$basePath\src\FichaCosto.Service\FichaCosto.Service.csproj" `
    -c $Configuration `
    -r win-x64 `
    --self-contained false `
    -o $publishPath

# 2. Copiar scripts y SQL
Write-Host "`n[2/5] Copiando recursos..." -ForegroundColor Yellow
Copy-Item "$installerPath\SeedDatabase.ps1" $publishPath -Force
Copy-Item "$basePath\src\FichaCosto.Service\Data\*.sql" "$publishPath\Data\" -Force

# 3. Compilar MSI (x64) con WiX v6.0.2
Write-Host "`n[3/5] Compilando MSI x64..." -ForegroundColor Yellow
wix build "$installerPath\Package.wxs" `
    -o "$installerPath\FichaCostoService-Setup-v$Version.msi" `
    -arch x64 `
    -d PublishDir=$publishPath `
    -ext WixToolset.Util.wixext `
    -ext WixToolset.Firewall.wixext

# 4. Compilar Bundle
Write-Host "`n[4/5] Compilando Bundle..." -ForegroundColor Yellow
wix build "$installerPath\Bundle.wxs" `
    -o "$installerPath\FichaCostoService-Bundle-v$Version.exe" `
    -arch x64 `
    -ext WixToolset.Bal.wixext `
    -ext WixToolset.Util.wixext

# 5. Verificar
Write-Host "`n[5/5] Verificando..." -ForegroundColor Yellow
Get-Item "$installerPath\*.msi", "$installerPath\*.exe" | Select Name, @{N="SizeMB";E={[math]::Round($_.Length/1MB,2)}}

Write-Host "`n✅ Build completado" -ForegroundColor Green
```

---

**¿Usamos este formato de contexto para las próximas sesiones? ¿O prefieres ajustar algo de las correcciones de arriba?**
