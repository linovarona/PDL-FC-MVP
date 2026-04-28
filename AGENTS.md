# AGENTS.md - FichaCosto Service

## Build & Test

```powershell
# Build completo de la solución
dotnet build FichaCosto.sln

# Ejecutar tests
dotnet test

# Publicar para release
dotnet publish src/FichaCosto.Service -c Release -o ./publish
```

## Notas Importantes

- **Archivo de solución**: `FichaCosto.sln` (raíz)
- **Proyecto principal**: `src/FichaCosto.Service` - REST API + Windows Service (.NET 9.0)
- **Tests**: `tests/FichaCosto.Service.Tests/`
- **Instalador**: `src/FichaCosto.Installer/` - WiX Bundle/MSI

## Estrategia de Ramas

- **master**: Código production-ready
- **develop**: Desarrollo activo
- Merge a master con tags: `git tag -a v0.x.x -m "mensaje"`

## Particularidades

- Solución solo para Windows (Windows Service, scripts PowerShell)
- Paquetes NuGet en carpeta `NuGetLocal/` para restore offline
- Base de datos SQLite para persistencia
- Servicio corre en `http://localhost:5000`
- Swagger disponible en `/swagger`