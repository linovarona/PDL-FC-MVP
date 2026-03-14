# FichaCosto Service - MVP

Sistema de automatización de fichas de costo para PyMEs según Resoluciones 148/2023 y 209/2024.

## 🎯 Alcance MVP (v1.0.0)

Este MVP incluye las funcionalidades críticas:

- ✅ **Cálculo de costos directos** (materias primas + mano de obra)
- ✅ **Validación de márgenes de utilidad** (máximo 30% según Res. 209/2024)
- ✅ **Importación desde Excel** (carga de datos de productos)
- ✅ **Exportación a Excel** (generación de fichas oficiales)

## 🚀 Requisitos

- Windows 10/11 Pro o Enterprise (64-bit)
- .NET 8.0 Runtime
- Microsoft Excel 2016+ (para import/export)
- 100 MB espacio en disco
- Puerto 5000 disponible

## 📦 Instalación Rápida

### Opción 1: Instalador MSI (Recomendado)

1. Descargar `FichaCostoService-Setup-v1.0.0.msi` de Releases
2. Ejecutar como Administrador
3. Seguir el asistente de instalación
4. El servicio se iniciará automáticamente

```powershell
# Instalación silenciosa
msiexec /i FichaCostoService-Setup-v1.0.0.msi /quiet
```

### Opción 2: Instalación Manual

```powershell
# Clonar repositorio
git clone https://github.com/linovarona/PDL-FC.git
cd PDL-FC

# Publicar
dotnet publish src/FichaCosto.Service -c Release -o ./publish

# Instalar servicio Windows
sc create FichaCostoService binPath= "C:\path\to\publish\FichaCosto.Service.exe --environment Production" start= auto
sc start FichaCostoService
```

## 🛠️ Uso

### API REST

Una vez instalado, la API está disponible en:
```
http://localhost:5000/api
```

### Endpoints MVP

| Funcionalidad | Endpoint | Descripción |
|--------------|----------|-------------|
| **Calcular Costos** | `POST /api/costos/calcular` | Calcula costos directos e indirectos |
| **Validar Margen** | `POST /api/costos/validar` | Valida el 30% máximo de utilidad |
| **Importar Excel** | `POST /api/excel/importar` | Carga datos desde Excel |
| **Exportar Excel** | `POST /api/excel/exportar` | Genera ficha oficial en Excel |

### Documentación Swagger

```
http://localhost:5000/swagger
```

## 📋 Estructura del Proyecto

```
PDL-FC/
├── src/
│   ├── FichaCosto.Service/          # API REST + Windows Service
│   └── FichaCosto.Installer/        # Instalador WiX (Fase 7)
├── tests/
│   └── FichaCosto.Service.Tests/    # Tests unitarios
├── scripts/
│   ├── build.ps1                    # Build completo
│   ├── test.ps1                     # Ejecutar tests
│   └── publish.ps1                  # Publicar para release
└── docs/
    ├── DOCUMENTACION_TECNICA.md
    └── PROCEDIMIENTO-FASE-01.md     # Guía de configuración inicial
```

## 🧪 Desarrollo

```powershell
# Build
.\scripts\build.ps1

# Tests
.\scripts\test.ps1

# Publicar
.\scripts\publish.ps1
```

## 🏛️ Normativa Aplicable

- **Resolución 148/2023**: Metodología de costos
- **Resolución 209/2024**: Márgenes de utilidad (máximo 30%)

## 📞 Soporte

- Documentación Técnica: `docs/DOCUMENTACION_TECNICA.md`
- Procedimiento Fase 1: `docs/PROCEDIMIENTO-FASE-01.md`
- Issues: https://github.com/linovarona/PDL-FC/issues

---

**Versión:** 1.0.0-MVP  
**Fecha:** Marzo 2026  
**Stack:** .NET 8.0, SQLite, Windows Service